using BoslaPlatform.Application.Features.RecordingAccess.Dtos;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.RecordingAccess.Services;

public sealed class RecordingAccessService : IRecordingAccessService
{
    private readonly IAppDbContext _context;
    private readonly IObjectStorage _objectStorage;
    private readonly PresignedUrlCache _urlCache;
    private readonly ILogger<RecordingAccessService> _logger;

    public RecordingAccessService(
        IAppDbContext context,
        IObjectStorage objectStorage,
        PresignedUrlCache urlCache,
        ILogger<RecordingAccessService> logger)
    {
        _context = context;
        _objectStorage = objectStorage;
        _urlCache = urlCache;
        _logger = logger;
    }

    public async Task<Result<RecordingWatchResponse>> GetWatchUrlAsync(
        Guid sessionId,
        Guid userId,
        string userRole,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var expirationTime = expiration ?? TimeSpan.FromMinutes(15);

        var session = await _context.VideoSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            _logger.LogWarning(
                "Watch URL requested for non-existent session {SessionId} by user {UserId}",
                sessionId, userId);

            return Error.NotFound(
                "Recording.NotFound",
                "Recording not found.");
        }

        if (session.UploadStatus != UploadStatus.Uploaded
            || string.IsNullOrWhiteSpace(session.ObjectKey)
            || string.IsNullOrWhiteSpace(session.BucketName))
        {
            _logger.LogWarning(
                "Watch URL requested for session {SessionId} with status {Status} by user {UserId}",
                sessionId, session.UploadStatus, userId);

            return Error.NotFound(
                "Recording.NotFound",
                "Recording not found.");
        }

        var appointment = await _context.Set<Domain.Models.Booking.Appointment>()
            .FirstOrDefaultAsync(a => a.Id == session.AppointmentId, ct);

        if (appointment is null)
        {
            _logger.LogWarning(
                "Appointment {AppointmentId} not found for session {SessionId}",
                session.AppointmentId, sessionId);

            return Error.NotFound(
                "Recording.NotFound",
                "Recording not found.");
        }
        var isOwner = appointment.UserId == userId;
        var isSpecialist = appointment.SpecialistId == userId;
        var isAdmin = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isSpecialist && !isAdmin)
        {
            _logger.LogWarning(
                "Access denied: user {UserId} (role {Role}) attempted to watch recording for session {SessionId}",
                userId, userRole, sessionId);

            return Error.Forbidden(
                "Recording.AccessDenied",
                "You do not have permission to access this recording.");
        }

        var cacheKey = $"{session.BucketName}:{session.ObjectKey}:{expirationTime.Ticks}";
        var cached = _urlCache.Get(cacheKey);

        if (cached is not null)
        {
            _logger.LogInformation(
                "Watch URL served from cache for session {SessionId}, objectKey={ObjectKey}",
                sessionId, session.ObjectKey);

            return new RecordingWatchResponse(
                cached.Url,
                cached.ExpiresAt,
                session.ContentType ?? "application/octet-stream",
                session.ContentLength,
                null);
        }

        var urlResult = await _objectStorage.GeneratePresignedUrlAsync(
            session.BucketName,
            session.ObjectKey,
            expirationTime,
            ct);

        if (urlResult.IsError)
        {
            _logger.LogError(
                "Failed to generate presigned URL for session {SessionId}, bucket={Bucket}, key={ObjectKey}: {Error}",
                sessionId, session.BucketName, session.ObjectKey,
                string.Join("; ", urlResult.Errors.Select(e => e.Description)));

            return Error.Failure(
                "Recording.UrlGenerationFailed",
                "Failed to generate access URL.");
        }

        var expiresAt = DateTime.UtcNow.Add(expirationTime);
        _urlCache.Set(cacheKey, urlResult.Value, expiresAt);

        _logger.LogInformation(
            "Watch URL generated for session {SessionId}, objectKey={ObjectKey}, storageProvider={Provider}, expiresAt={ExpiresAt}",
            sessionId, session.ObjectKey, session.StorageProvider, expiresAt);

        return new RecordingWatchResponse(
            urlResult.Value,
            expiresAt,
            session.ContentType ?? "application/octet-stream",
            session.ContentLength,
            null);
    }
}