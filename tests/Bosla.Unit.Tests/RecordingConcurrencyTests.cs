using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingConcurrencyTests
{
    // ── OptimisticRecordingLock ───────────────────────────────────────────────

    [Fact]
    public async Task Lock_first_acquire_succeeds()
    {
        var @lock = new OptimisticRecordingLock();
        var sessionId = Guid.NewGuid();

        var handle = await @lock.TryAcquireAsync(sessionId);

        Assert.NotNull(handle);
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task Lock_second_acquire_returns_null_while_first_is_held()
    {
        var @lock = new OptimisticRecordingLock();
        var sessionId = Guid.NewGuid();

        var first = await @lock.TryAcquireAsync(sessionId);
        Assert.NotNull(first);

        // Second acquire for same session must fail
        var second = await @lock.TryAcquireAsync(sessionId);
        Assert.Null(second);

        await first!.DisposeAsync();
    }

    [Fact]
    public async Task Lock_can_be_re_acquired_after_release()
    {
        var @lock = new OptimisticRecordingLock();
        var sessionId = Guid.NewGuid();

        var first = await @lock.TryAcquireAsync(sessionId);
        Assert.NotNull(first);
        await first!.DisposeAsync();

        // After release, the lock should be available again
        var second = await @lock.TryAcquireAsync(sessionId);
        Assert.NotNull(second);
        await second!.DisposeAsync();
    }

    [Fact]
    public async Task Lock_different_sessions_can_be_locked_simultaneously()
    {
        var @lock = new OptimisticRecordingLock();
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();

        var handle1 = await @lock.TryAcquireAsync(session1);
        var handle2 = await @lock.TryAcquireAsync(session2);

        Assert.NotNull(handle1);
        Assert.NotNull(handle2);

        await handle1!.DisposeAsync();
        await handle2!.DisposeAsync();
    }

    [Fact]
    public async Task Lock_dispose_is_idempotent()
    {
        var @lock = new OptimisticRecordingLock();
        var sessionId = Guid.NewGuid();

        var handle = await @lock.TryAcquireAsync(sessionId);
        Assert.NotNull(handle);

        // Dispose twice — must not throw
        await handle!.DisposeAsync();
        await handle.DisposeAsync();

        // Lock must be released after first dispose
        var next = await @lock.TryAcquireAsync(sessionId);
        Assert.NotNull(next);
        await next!.DisposeAsync();
    }

    [Fact]
    public async Task Concurrent_acquires_for_same_session_only_one_succeeds()
    {
        var @lock = new OptimisticRecordingLock();
        var sessionId = Guid.NewGuid();

        // Simulate 10 concurrent tasks trying to acquire the same lock
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => @lock.TryAcquireAsync(sessionId))
            .ToList();

        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(r => r != null);
        var failCount = results.Count(r => r == null);

        // Exactly one should succeed
        Assert.Equal(1, successCount);
        Assert.Equal(9, failCount);

        // Clean up the successful handle
        var winner = results.First(r => r != null);
        await winner!.DisposeAsync();
    }

    // ── NoOpRecordingLock ─────────────────────────────────────────────────────

    [Fact]
    public async Task NoOpLock_always_succeeds_regardless_of_session()
    {
        var @lock = new NoOpRecordingLock();
        var session = Guid.NewGuid();

        var handle1 = await @lock.TryAcquireAsync(session);
        var handle2 = await @lock.TryAcquireAsync(session); // Same session, different call

        Assert.NotNull(handle1);
        Assert.NotNull(handle2);

        await handle1!.DisposeAsync();
        await handle2!.DisposeAsync();
    }
}
