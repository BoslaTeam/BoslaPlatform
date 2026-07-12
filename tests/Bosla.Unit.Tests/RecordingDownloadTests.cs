using BoslaPlatform.Application.Features.RecordingAccess.Dtos;
using BoslaPlatform.Application.Features.RecordingAccess.Services;
using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BoslaPlatform.Domain.Enums.StorageProvider;

namespace Bosla.Unit.Tests;

/// <summary>
/// Tests for the recording download pipeline: authorization, streaming, and error cases.
/// Complements <see cref="RecordingAccessServiceTests"/> which covers the watch flow.
/// </summary>
public class RecordingDownloadTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IObjectStorage> _storageMock;
    private readonly RecordingAccessService _service;
    private readonly Appointment _appointment;
    private readonly VideoSession _session;

    public RecordingDownloadTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RecordingDownloadTest_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _storageMock = new Mock<IObjectStorage>();
        var loggerMock = Mock.Of<ILogger<RecordingAccessService>>();
        var cache = new PresignedUrlCache();

        _appointment = Appointment.Schedule(
            specialistId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            start: DateTimeOffset.UtcNow,
            end: DateTimeOffset.UtcNow.AddHours(1),
            sessionTopic: null,
            notes: null,
            sessionPrice: 100m);

        _context.Appointments.Add(_appointment);
        _context.SaveChanges();

        var sessionResult = VideoSession.Create(
            _appointment.Id, "channel-dl-test", "app-id", VideoSessionType.OneToOne);
        _session = sessionResult.Value;

        _session.MarkUploadSucceeded(
            CloudflareR2,
            "bosla-recordings",
            $"{_session.Id}/recording.mp4",
            "video/mp4",
            52_428_800L); // 50 MB

        _context.VideoSessions.Add(_session);
        _context.SaveChanges();

        var auditMock = Mock.Of<IRecordingAuditService>();
        var metricsMock = new NoOpRecordingMetrics();
        var settingsMock = new Mock<IRecordingStorageSettings>();
        settingsMock.Setup(s => s.PresignedUrlExpirationMinutes).Returns(15);

        _service = new RecordingAccessService(
            _context, 
            _storageMock.Object, 
            cache, 
            auditMock,
            metricsMock,
            settingsMock.Object,
            loggerMock);
    }

    public void Dispose() => _context.Dispose();

    // ── Authorization tests ────────────────────────────────────────────────

    [Fact]
    public async Task Owner_can_download_recording()
    {
        ArrangeStorageStream();

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Content);
        Assert.Equal("recording.mp4", result.Value.FileName);
        result.Value.Content.Dispose();
    }

    [Fact]
    public async Task Specialist_can_download_recording()
    {
        ArrangeStorageStream();

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, _appointment.SpecialistId, "Specialist");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Content);
        result.Value.Content.Dispose();
    }

    [Fact]
    public async Task Admin_can_download_any_recording()
    {
        ArrangeStorageStream();
        var adminId = Guid.NewGuid(); // not part of the appointment

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, adminId, "Admin");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Content);
        result.Value.Content.Dispose();
    }

    [Fact]
    public async Task Unrelated_user_gets_forbidden()
    {
        var unrelatedUserId = Guid.NewGuid();

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, unrelatedUserId, "User");

        Assert.True(result.IsError);
        Assert.Equal("Recording.AccessDenied", result.Errors[0].Code);

        // Storage must NOT be called — never open a stream for unauthorised requests.
        _storageMock.Verify(
            x => x.OpenReadStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Not-found / status guard tests ────────────────────────────────────

    [Fact]
    public async Task Returns_not_found_for_non_existent_session()
    {
        var result = await _service.GetDownloadStreamAsync(
            Guid.NewGuid(), _appointment.UserId, "User");

        Assert.True(result.IsError);
        Assert.Equal("Recording.NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Returns_not_found_when_upload_not_completed()
    {
        // Use a separate appointment to avoid EF in-memory tracker conflicts
        // with the shared _appointment that already has a tracked VideoSession.
        var otherAppointment = Appointment.Schedule(
            specialistId: Guid.NewGuid(),
            userId: _appointment.UserId,  // same user — auth will pass
            start: DateTimeOffset.UtcNow,
            end: DateTimeOffset.UtcNow.AddHours(1),
            sessionTopic: null,
            notes: null,
            sessionPrice: 100m);
        _context.Appointments.Add(otherAppointment);
        await _context.SaveChangesAsync();

        // Session NOT marked as Uploaded — UploadStatus defaults to Pending.
        var pendingSessionResult = VideoSession.Create(
            otherAppointment.Id, "pending-channel", "app-id", VideoSessionType.OneToOne);
        var pendingSession = pendingSessionResult.Value;
        _context.VideoSessions.Add(pendingSession);
        await _context.SaveChangesAsync();

        var result = await _service.GetDownloadStreamAsync(
            pendingSession.Id, _appointment.UserId, "User");

        Assert.True(result.IsError);
        Assert.Equal("Recording.NotFound", result.Errors[0].Code);
    }

    // ── Storage failure tests ──────────────────────────────────────────────

    [Fact]
    public async Task Returns_failure_when_storage_stream_fails()
    {
        _storageMock
            .Setup(x => x.OpenReadStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Storage.StreamOpenFailed", "Network error."));

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.True(result.IsError);
        Assert.Equal("Recording.StreamFailed", result.Errors[0].Code);
    }

    // ── Content metadata tests ─────────────────────────────────────────────

    [Fact]
    public async Task Download_result_carries_correct_content_metadata()
    {
        ArrangeStorageStream();

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.False(result.IsError);

        var ctx = result.Value;
        Assert.Equal("video/mp4", ctx.ContentType);
        Assert.Equal(52_428_800L, ctx.ContentLength);
        Assert.Equal("recording.mp4", ctx.FileName);

        ctx.Content.Dispose();
    }

    [Fact]
    public async Task Download_filename_is_derived_from_object_key()
    {
        ArrangeStorageStream();

        var result = await _service.GetDownloadStreamAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.False(result.IsError);
        // ObjectKey is "{sessionId}/recording.mp4", filename should be the last segment.
        Assert.Equal("recording.mp4", result.Value.FileName);
        result.Value.Content.Dispose();
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private void ArrangeStorageStream()
    {
        var fakeStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });

        _storageMock
            .Setup(x => x.OpenReadStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadObjectResponse(fakeStream, "video/mp4", fakeStream.Length));
    }
}
