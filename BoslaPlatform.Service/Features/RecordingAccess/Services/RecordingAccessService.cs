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
    private readonly IRecordingAuditService _audit;
    private readonly IRecordingMetrics _metrics;
    private readonly IRecordingStorageSettings _storageSettings;
    private readonly ILogger<RecordingAccessService> _logger;

    public RecordingAccessService(
        IAppDbContext context,
        IObjectStorage objectStorage,
        PresignedUrlCache urlCache,
        IRecordingAuditService audit,
        IRecordingMetrics metrics,
        IRecordingStorageSettings storageSettings,
        ILogger<RecordingAccessService> logger)
    {
        _context = context;
        _objectStorage = objectStorage;
        _urlCache = urlCache;
        _audit = audit;
        _metrics = metrics;
        _storageSettings = storageSettings;
        _logger = logger;
    }

    public async Task<Result<RecordingWatchResponse>> GetWatchUrlAsync(
        Guid sessionId,
        Guid userId,
        string userRole,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        // Clamp expiration to configured maximum — never allow an unbounded URL lifetime.
        var configuredMax = TimeSpan.FromMinutes(_storageSettings.PresignedUrlExpirationMinutes);
        var expirationTime = expiration.HasValue
            ? TimeSpan.FromTicks(Math.Min(expiration.Value.Ticks, configuredMax.Ticks))
            : configuredMax;

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
                GetFileName(session.ObjectKey),
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

        _metrics.RecordPresignedUrlGenerated();

        _logger.LogInformation(
            "Watch URL generated for session {SessionId}, objectKey={ObjectKey}, storageProvider={Provider}, expiresAt={ExpiresAt}",
            sessionId, session.ObjectKey, session.StorageProvider, expiresAt);

        // Fire-and-forget audit log — never blocks the response.
        await _audit.LogAsync(sessionId, userId, RecordingAuditAction.Viewed, ct);

        return new RecordingWatchResponse(
            urlResult.Value,
            expiresAt,
            session.ContentType ?? "application/octet-stream",
            session.ContentLength,
            GetFileName(session.ObjectKey),
            null);
    }

    public async Task<Result<RecordingDownloadContext>> GetDownloadStreamAsync(
        Guid sessionId,
        Guid userId,
        string userRole,
        CancellationToken ct = default)
    {
        var session = await _context.VideoSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            _logger.LogWarning(
                "Download requested for non-existent session {SessionId} by user {UserId}",
                sessionId, userId);

            // Return NotFound for both "session missing" and "not yet uploaded" to avoid
            // leaking information about whether a recording exists.
            return Error.NotFound("Recording.NotFound", "Recording not found.");
        }

        if (session.UploadStatus != UploadStatus.Uploaded
            || string.IsNullOrWhiteSpace(session.ObjectKey)
            || string.IsNullOrWhiteSpace(session.BucketName))
        {
            _logger.LogWarning(
                "Download requested for session {SessionId} with upload status {Status}",
                sessionId, session.UploadStatus);

            return Error.NotFound("Recording.NotFound", "Recording not found.");
        }

        var appointment = await _context.Set<Domain.Models.Booking.Appointment>()
            .FirstOrDefaultAsync(a => a.Id == session.AppointmentId, ct);

        if (appointment is null)
        {
            _logger.LogWarning(
                "Appointment {AppointmentId} not found for session {SessionId}",
                session.AppointmentId, sessionId);

            return Error.NotFound("Recording.NotFound", "Recording not found.");
        }

        var isOwner      = appointment.UserId      == userId;
        var isSpecialist = appointment.SpecialistId == userId;
        var isAdmin      = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isSpecialist && !isAdmin)
        {
            _logger.LogWarning(
                "Download access denied: user {UserId} (role {Role}) attempted to download recording for session {SessionId}",
                userId, userRole, sessionId);

            return Error.Forbidden(
                "Recording.AccessDenied",
                "You do not have permission to download this recording.");
        }

        var streamResult = await _objectStorage.OpenReadStreamAsync(
            session.BucketName,
            session.ObjectKey,
            ct);

        if (streamResult.IsError)
        {
            _logger.LogError(
                "Failed to open storage stream for session {SessionId}, bucket={Bucket}, key={ObjectKey}: {Error}",
                sessionId, session.BucketName, session.ObjectKey,
                string.Join("; ", streamResult.Errors.Select(e => e.Description)));

            return Error.Failure(
                "Recording.StreamFailed",
                "Failed to open recording stream.");
        }

        var fileName = GetFileName(session.ObjectKey) ?? "recording.mp4";

        _logger.LogInformation(
            "Download stream opened for session {SessionId}, objectKey={ObjectKey}, contentLength={ContentLength}",
            sessionId, session.ObjectKey, session.ContentLength);

        // Fire-and-forget audit log.
        await _audit.LogAsync(sessionId, userId, RecordingAuditAction.Downloaded, ct);

        return new RecordingDownloadContext(
            streamResult.Value.Content,
            session.ContentType ?? "application/octet-stream",
            session.ContentLength ?? streamResult.Value.ContentLength,
            fileName);
    }

    private static string? GetFileName(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var normalized = objectKey.Replace('\\', '/');
        var separatorIndex = normalized.LastIndexOf('/');
        return separatorIndex >= 0
            ? normalized[(separatorIndex + 1)..]
            : normalized;
    }
}
