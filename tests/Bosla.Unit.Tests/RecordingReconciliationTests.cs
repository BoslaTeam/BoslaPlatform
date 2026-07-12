using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingReconciliationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IRecordingProvider> _providerMock;
    private readonly Mock<IRecordingLock> _lockMock;
    private readonly RecordingReconciliationOptions _options;

    public RecordingReconciliationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ReconciliationTest_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(dbOptions);

        _providerMock = new Mock<IRecordingProvider>();
        _lockMock = new Mock<IRecordingLock>();

        // Default: lock always succeeds
        _lockMock.Setup(x => x.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NoOpDisposable());

        _options = new RecordingReconciliationOptions
        {
            MaxRetryAttempts = 3,
            BaseBackoffSeconds = 1,
            BatchSize = 10,
            PollingIntervalSeconds = 60
        };
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task ReconcileAsync_skips_sessions_with_RetryCount_at_max()
    {
        // Arrange: session at max retry count — should NOT be queried from Agora
        var session = CreateSession(UploadStatus.Retrying, retryCount: 3);
        _context.VideoSessions.Add(session);
        await _context.SaveChangesAsync();

        var service = BuildService();
        await service.TestReconcileAsync(CancellationToken.None);

        _providerMock.Verify(p => p.QueryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_skips_sessions_where_NextRetryAtUtc_is_in_future()
    {
        // Arrange: session with future retry time — not due yet
        var session = CreateSession(UploadStatus.Retrying, nextRetryAt: DateTime.UtcNow.AddHours(1));
        _context.VideoSessions.Add(session);
        await _context.SaveChangesAsync();

        var service = BuildService();
        await service.TestReconcileAsync(CancellationToken.None);

        _providerMock.Verify(p => p.QueryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_acquires_lock_per_eligible_session()
    {
        var session = CreateSession(UploadStatus.Pending);
        _context.VideoSessions.Add(session);
        await _context.SaveChangesAsync();

        _providerMock.Setup(p => p.QueryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult(RecordingStatus.Processing, "r", "s", null, null));

        var service = BuildService();
        await service.TestReconcileAsync(CancellationToken.None);

        _lockMock.Verify(l => l.TryAcquireAsync(session.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_skips_session_when_lock_unavailable()
    {
        var session = CreateSession(UploadStatus.Pending);
        _context.VideoSessions.Add(session);
        await _context.SaveChangesAsync();

        // Lock returns null → session is already being processed by another task
        _lockMock.Setup(x => x.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var service = BuildService();
        await service.TestReconcileAsync(CancellationToken.None);

        _providerMock.Verify(p => p.QueryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_session_without_agora_ids_is_skipped()
    {
        // Session is Pending but has no Agora IDs — cannot be reconciled
        var session = CreateSession(UploadStatus.Pending, hasAgoraIds: false);
        _context.VideoSessions.Add(session);
        await _context.SaveChangesAsync();

        var service = BuildService();
        await service.TestReconcileAsync(CancellationToken.None);

        _providerMock.Verify(p => p.QueryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private VideoSession CreateSession(
        UploadStatus status,
        int retryCount = 0,
        DateTime? nextRetryAt = null,
        bool hasAgoraIds = true)
    {
        // Build a minimal VideoSession bypassing the private constructor.
        var session = (VideoSession)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(VideoSession));

        Set(session, nameof(VideoSession.Id), Guid.NewGuid());
        Set(session, nameof(VideoSession.AppointmentId), Guid.NewGuid());
        Set(session, nameof(VideoSession.UploadStatus), status);
        Set(session, nameof(VideoSession.RetryCount), retryCount);
        Set(session, nameof(VideoSession.NextRetryAtUtc), nextRetryAt);
        Set(session, nameof(VideoSession.AgoraRecordingId), hasAgoraIds ? "resource-123" : null);
        Set(session, nameof(VideoSession.AgoraRecordingSid), hasAgoraIds ? "sid-123" : null);
        Set(session, nameof(VideoSession.AgoraChannelName), $"channel-{Guid.NewGuid():N}");
        Set(session, nameof(VideoSession.AgoraAppId), "app-id");
        Set(session, nameof(VideoSession.Status), VideoSessionStatus.Ended);

        return session;
    }

    private static void Set(object obj, string propName, object? value)
        => obj.GetType().GetProperty(propName)!.SetValue(obj, value);

    private TestableReconciliationService BuildService()
    {
        var scopeFactory = BuildScopeFactory();
        return new TestableReconciliationService(
            scopeFactory,
            Options.Create(_options),
            _lockMock.Object,
            Mock.Of<ILogger<BoslaPlatform.Infrastructure.BackgroundJobs.RecordingReconciliationService>>());
    }

    private IServiceScopeFactory BuildScopeFactory()
    {
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(BoslaPlatform.Application.Interfaces.Persistence.IAppDbContext)))
            .Returns(_context);
        spMock.Setup(sp => sp.GetService(typeof(IRecordingProvider)))
            .Returns(_providerMock.Object);
        spMock.Setup(sp => sp.GetService(typeof(IRecordingMetrics)))
            .Returns(new NoOpRecordingMetrics());
        // Transfer service not needed for these tests — reconciliation only gets to it if files are ready
        spMock.Setup(sp => sp.GetService(
                typeof(BoslaPlatform.Application.Features.RecordingTransfer.Services.RecordingTransferService)))
            .Returns(null!);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        return factoryMock.Object;
    }

    private sealed class NoOpDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>
/// Thin subclass that exposes the protected reconcile pass for unit testing.
/// </summary>
public sealed class TestableReconciliationService
    : BoslaPlatform.Infrastructure.BackgroundJobs.RecordingReconciliationService
{
    public TestableReconciliationService(
        IServiceScopeFactory scopeFactory,
        IOptions<RecordingReconciliationOptions> options,
        IRecordingLock @lock,
        ILogger<BoslaPlatform.Infrastructure.BackgroundJobs.RecordingReconciliationService> logger)
        : base(scopeFactory, options, @lock, logger) { }

    public Task TestReconcileAsync(CancellationToken ct)
        => ReconcileInternalAsync(ct);
}
