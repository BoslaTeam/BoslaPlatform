using BoslaPlatform.Application.Features.RecordingAccess.Dtos;
using BoslaPlatform.Application.Features.RecordingAccess.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BoslaPlatform.Domain.Enums.StorageProvider;

namespace Bosla.Unit.Tests;

public class RecordingAccessServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IObjectStorage> _storageMock;
    private readonly PresignedUrlCache _cache;
    private readonly RecordingAccessService _service;
    private readonly Appointment _appointment;
    private readonly VideoSession _session;

    public RecordingAccessServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RecordingAccessTest_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _storageMock = new Mock<IObjectStorage>();
        _cache = new PresignedUrlCache();
        var loggerMock = Mock.Of<ILogger<RecordingAccessService>>();

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

        var result = VideoSession.Create(
            _appointment.Id, "channel-test", "app-id", VideoSessionType.OneToOne);
        _session = result.Value;
        _session.MarkUploadSucceeded(
            CloudflareR2,
            "recordings",
            "session-1/recording.m3u8",
            "application/vnd.apple.mpegurl",
            1024L);

        _context.VideoSessions.Add(_session);
        _context.SaveChanges();

        var auditMock = Mock.Of<IRecordingAuditService>();
        var metricsMock = new NoOpRecordingMetrics();
        var settingsMock = new Mock<IRecordingStorageSettings>();
        settingsMock.Setup(s => s.PresignedUrlExpirationMinutes).Returns(15);

        _service = new RecordingAccessService(
            _context,
            _storageMock.Object,
            _cache,
            auditMock,
            metricsMock,
            settingsMock.Object,
            loggerMock);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Owner_can_watch_recording()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.False(result.IsError);
        Assert.IsType<RecordingWatchResponse>(result.Value);
    }

    [Fact]
    public async Task Specialist_can_watch_recording()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, _appointment.SpecialistId, "Specialist");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.PresignedUrl);
    }

    [Fact]
    public async Task Admin_can_watch_recording()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, Guid.NewGuid(), "Admin");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.PresignedUrl);
    }

    [Fact]
    public async Task Unauthorized_user_gets_forbidden()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, Guid.NewGuid(), "User");

        Assert.True(result.IsError);
        Assert.Contains("AccessDenied", result.Errors[0].Code);
    }

    [Fact]
    public async Task Non_existent_session_returns_not_found()
    {
        var result = await _service.GetWatchUrlAsync(
            Guid.NewGuid(), Guid.NewGuid(), "User");

        Assert.True(result.IsError);
        Assert.Contains("NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Not_uploaded_session_returns_not_found()
    {
        var context = CreateSecondContext();
        var auditMock = Mock.Of<IRecordingAuditService>();
        var metricsMock = new NoOpRecordingMetrics();
        var settingsMock = new Mock<IRecordingStorageSettings>();
        settingsMock.Setup(s => s.PresignedUrlExpirationMinutes).Returns(15);

        var service = new RecordingAccessService(
            context,
            _storageMock.Object,
            _cache,
            auditMock,
            metricsMock,
            settingsMock.Object,
            Mock.Of<ILogger<RecordingAccessService>>());

        var unUploadedResult = VideoSession.Create(
            Guid.NewGuid(), "ch", "app", VideoSessionType.OneToOne);
        var unUploaded = unUploadedResult.Value;

        var otherAppt = Appointment.Schedule(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            null, null, 50m);
        context.Appointments.Add(otherAppt);

        var field = typeof(VideoSession).GetProperty(nameof(VideoSession.AppointmentId))!;
        field.SetValue(unUploaded, otherAppt.Id);
        context.VideoSessions.Add(unUploaded);
        context.SaveChanges();

        var result = await service.GetWatchUrlAsync(
            unUploaded.Id, otherAppt.UserId, "User");

        Assert.True(result.IsError);
        Assert.Contains("NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Presigned_url_cached_on_subsequent_calls()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url")
            .Verifiable();

        await _service.GetWatchUrlAsync(
            _session.Id, _appointment.UserId, "User", TimeSpan.FromMinutes(15));

        await _service.GetWatchUrlAsync(
            _session.Id, _appointment.UserId, "User", TimeSpan.FromMinutes(15));

        _storageMock.Verify(
            x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Response_contains_content_type_and_length()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, _appointment.UserId, "User");

        Assert.False(result.IsError);
        Assert.Equal("application/vnd.apple.mpegurl", result.Value.ContentType);
        Assert.Equal(1024L, result.Value.ContentLength);
        Assert.True(result.Value.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task GeneratePresignedUrlAsync_called_on_cache_miss()
    {
        _storageMock.Setup(x => x.GeneratePresignedUrlAsync(
                "recordings", "session-1/recording.m3u8",
                TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://generated.url");

        var result = await _service.GetWatchUrlAsync(
            _session.Id, _appointment.UserId, "User", TimeSpan.FromMinutes(15));

        Assert.False(result.IsError);
        _storageMock.Verify(
            x => x.GeneratePresignedUrlAsync(
                "recordings", "session-1/recording.m3u8",
                TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AppDbContext CreateSecondContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RecordingAccessTest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}