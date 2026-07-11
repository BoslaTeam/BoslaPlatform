using BoslaPlatform.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace BoslaPlatform.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// An EF Core <see cref="SaveChangesInterceptor"/> that publishes local Domain Events
    /// through MediatR <b>after</b> <see cref="DbContext.SaveChangesAsync()"/> has
    /// successfully committed the transaction (in <c>SavedChanges</c>).
    ///
    /// This implements the <b>Local Domain Events</b> side of the
    /// Hybrid Domain Event Architecture:
    ///
    /// ┌──────────────────────────────────────────────────────────────────────────┐
    /// │  Hybrid Domain Event Architecture                                       │
    /// ├──────────────────────────────────────────────────────────────────────────┤
    /// │  BEFORE COMMIT  │  OutboxSaveChangesInterceptor                          │
    /// │  (SavingChanges) │  → Serialises domain events → persists to Outbox      │
    /// │                  │  → Does NOT touch DomainEvents collections            │
    /// │                  │  → Same transaction as domain data                    │
    /// ├──────────────────────────────────────────────────────────────────────────┤
    /// │  AFTER COMMIT   │  DomainEventsInterceptor (this)                       │
    /// │  (SavedChanges)  │  → Publishes each captured event via MediatR         │
    /// │                  │  → THEN clears DomainEvents from aggregates           │
    /// │                  │                                                       │
    /// │                  │  Why after commit?                                    │
    /// │                  │  - Handlers see a consistent, committed database      │
    /// │                  │  - If a handler fails, the transaction is NOT rolled  │
    /// │                  │    back (it already committed)                        │
    /// │                  │  - This prevents partial commits from partial handler  │
    /// │                  │    failures                                           │
    /// │                  │  - Handlers MUST be idempotent                        │
    /// └──────────────────────────────────────────────────────────────────────────┘
    ///
    /// <b>Event lifecycle:</b>
    ///   1. Aggregate method raises event → AddDomainEvent()
    ///   2. SaveChangesAsync → OutboxSaveChangesInterceptor serialises to Outbox
    ///   3. SaveChangesAsync → DomainEventsInterceptor captures snapshot (SavingChanges)
    ///   4. EF Core commits the transaction (OutboxMessages + domain data)
    ///   5. DomainEventsInterceptor publishes snapshot via MediatR (SavedChanges)
    ///   6. DomainEventsInterceptor clears DomainEvents from aggregates (SavedChanges)
    ///
    /// <b>Single owner of ClearDomainEvents:</b>
    /// This interceptor is the ONLY component that calls ClearDomainEvents().
    /// OutboxSaveChangesInterceptor does NOT clear events — it only reads and
    /// serialises. This ensures that Local Domain Events are always dispatched
    /// even when Integration Events are also being persisted.
    /// </summary>
    public sealed class DomainEventsInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Pending entries captured before SaveChanges, each with a snapshot
        /// of its domain events at that point in time.
        /// </summary>
        private readonly List<(BaseEntity Entity, List<DomainEvent> Events)> _pendingEntries = [];

        private readonly IPublisher _publisher;

        public DomainEventsInterceptor(IPublisher publisher)
        {
            _publisher = publisher;
        }

        #region SavingChanges

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CaptureDomainEvents(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureDomainEvents(eventData.Context);
            return ValueTask.FromResult(result);
        }

        #endregion

        #region SavedChanges

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (_pendingEntries.Count == 0)
                return result;

            // Snapshot and clear the pending list before publishing.
            // This ensures re-entrant SaveChanges (triggered by a handler)
            // do not see these events again.
            var entries = _pendingEntries.ToList();
            _pendingEntries.Clear();

            foreach (var (_, events) in entries)
            {
                foreach (var domainEvent in events)
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                }
            }

            // Clear domain events from aggregates only after successful publication.
            foreach (var (entity, _) in entries)
            {
                entity.ClearDomainEvents();
            }

            return result;
        }

        #endregion

        #region Failed

        public override void SaveChangesFailed(
            DbContextErrorEventData eventData)
        {
            _pendingEntries.Clear();
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _pendingEntries.Clear();
            return Task.CompletedTask;
        }

        #endregion

        private void CaptureDomainEvents(DbContext? context)
        {
            if (context is null)
                return;

            // Each SaveChanges starts with a clean staging list.
            _pendingEntries.Clear();

            var entities = context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e =>
                    e.State is EntityState.Added
                        or EntityState.Modified
                        or EntityState.Deleted)
                .Where(e => e.Entity.DomainEvents.Any())
                .ToList();

            foreach (var entry in entities)
            {
                // Take a snapshot so changes between SavingChanges and SavedChanges
                // do not affect what we publish.
                var snapshot = entry.Entity.DomainEvents.ToList();
                _pendingEntries.Add((entry.Entity, snapshot));
            }
        }
    }
}
