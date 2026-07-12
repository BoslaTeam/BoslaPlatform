namespace BoslaPlatform.Application.Interfaces.Storage;

/// <summary>
/// Provider-agnostic distributed lock abstraction for recording uploads.
/// Prevents two concurrent processes from uploading the same recording.
/// </summary>
public interface IRecordingLock
{
    /// <summary>
    /// Tries to acquire an exclusive lock for the given session.
    /// Returns a non-null <see cref="IAsyncDisposable"/> on success that releases the lock when disposed.
    /// Returns null if the lock could not be acquired (another process holds it).
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(Guid sessionId, CancellationToken ct = default);
}

/// <summary>
/// Null object implementation — useful in unit tests and single-instance deployments
/// that rely on EF Core optimistic concurrency as their sole concurrency guard.
/// </summary>
public sealed class NoOpRecordingLock : IRecordingLock
{
    public Task<IAsyncDisposable?> TryAcquireAsync(Guid sessionId, CancellationToken ct = default)
        => Task.FromResult<IAsyncDisposable?>(NullDisposable.Instance);

    private sealed class NullDisposable : IAsyncDisposable
    {
        public static readonly NullDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
