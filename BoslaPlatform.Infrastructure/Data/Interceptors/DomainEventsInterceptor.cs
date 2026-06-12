using BoslaPlatform.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoslaPlatform.Infrastructure.Data.Interceptors
{
    public sealed class DomainEventsInterceptor
    : SaveChangesInterceptor
    {
        private readonly IPublisher _publisher;

        public DomainEventsInterceptor(
            IPublisher publisher)
        {
            _publisher = publisher;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;

            if (context is null)
            {
                return result;
            }

            var entities = context.ChangeTracker
                .Entries<BaseEntity>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Any())
                .ToList();

            var domainEvents = entities
                .SelectMany(x => x.DomainEvents)
                .ToList();

            foreach (var entity in entities)
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(
                    domainEvent,
                    cancellationToken);
            }

            return result;
        }
    }
}
