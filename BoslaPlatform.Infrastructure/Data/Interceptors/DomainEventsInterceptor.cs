using BoslaPlatform.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace BoslaPlatform.Infrastructure.Data.Interceptors
{
    public sealed class DomainEventsInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Pending Domain Events captured before SaveChanges().
        /// IMPORTANT:
        /// This interceptor must be registered with Scoped lifetime.
        /// </summary>
        private readonly List<DomainEvent> _pendingDomainEvents = [];

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
            if (_pendingDomainEvents.Count == 0)
                return result;

            // Snapshot to avoid issues if a handler triggers another SaveChanges()
            var events = _pendingDomainEvents.ToList();

            _pendingDomainEvents.Clear();

            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }

            return result;
        }

        #endregion

        #region Failed

        public override void SaveChangesFailed(
            DbContextErrorEventData eventData)
        {
            _pendingDomainEvents.Clear();
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _pendingDomainEvents.Clear();
            return Task.CompletedTask;
        }

        #endregion

        private void CaptureDomainEvents(DbContext? context)
        {
            if (context is null)
                return;

            // Defensive programming.
            // Each SaveChanges starts with a clean staging list.
            _pendingDomainEvents.Clear();

            var entities = context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e =>
                    e.State is EntityState.Added
                        or EntityState.Modified
                        or EntityState.Deleted)
                .Where(e => e.Entity.DomainEvents.Any())
                .ToList();

            foreach (var entity in entities)
            {
                _pendingDomainEvents.AddRange(entity.Entity.DomainEvents);

                // Prevent duplicate publishing on subsequent SaveChanges().
                entity.Entity.ClearDomainEvents();
            }
        }
    }
}
