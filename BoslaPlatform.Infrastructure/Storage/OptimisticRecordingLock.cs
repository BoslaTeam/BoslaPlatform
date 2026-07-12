using System.Collections.Concurrent;
using BoslaPlatform.Application.Interfaces.Storage;

namespace BoslaPlatform.Infrastructure.Storage;

/// <summary>
/// Process-level recording lock using a ConcurrentDictionary as an in-memory guard.
/// Combines with EF Core RowVersion for full optimistic concurrency protection:
///   - ConcurrentDictionary: prevents two tasks within the SAME process.
///   - RowVersion: prevents two different processes (pods) from both committing.
/// </summary>
public sealed class OptimisticRecordingLock : IRecordingLock
{
    // Tracks session IDs currently being processed in this process.
    private readonly ConcurrentDictionary<Guid, byte> _activeLocks = new();

    /// <inheritdoc />
    public Task<IAsyncDisposable?> TryAcquireAsync(Guid sessionId, CancellationToken ct = default)
    {
        // TryAdd is atomic — only one thread/task can succeed for a given key.
        if (_activeLocks.TryAdd(sessionId, 0))
        {
            return Task.FromResult<IAsyncDisposable?>(new LockHandle(_activeLocks, sessionId));
        }

        // Another task in this process already owns the lock.
        return Task.FromResult<IAsyncDisposable?>(null);
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<Guid, byte> _activeLocks;
        private readonly Guid _sessionId;
        private bool _disposed;

        public LockHandle(ConcurrentDictionary<Guid, byte> activeLocks, Guid sessionId)
        {
            _activeLocks = activeLocks;
            _sessionId = sessionId;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _activeLocks.TryRemove(_sessionId, out _);
            }
            return ValueTask.CompletedTask;
        }
    }
}
