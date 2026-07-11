using BoslaPlatform.Domain.Common;
using BoslaPlatform.Infrastructure.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.Data.Interceptors;

/// <summary>
/// An EF Core <see cref="SaveChangesInterceptor"/> that persists serialised domain
/// events as <see cref="OutboxMessage"/> records <b>before</b> the database commit
/// (in <c>SavingChanges</c>).
///
/// This implements the <b>Integration Events (Outbox)</b> side of the
/// Hybrid Domain Event Architecture:
///
/// ┌──────────────────────────────────────────────────────────────────────────┐
/// │  Hybrid Domain Event Architecture                                       │
/// ├──────────────────────────────────────────────────────────────────────────┤
/// │  BEFORE COMMIT  │  OutboxSaveChangesInterceptor                         │
/// │  (SavingChanges) │  → Enumerates ChangeTracker for BaseEntity entries   │
/// │                  │  → Serialises each DomainEvent to JSON               │
/// │                  │  → Creates OutboxMessage and adds to DbSet            │
/// │                  │  → Domain events are NOT cleared here                 │
/// │                  │                                                       │
/// │                  │  Because the OutboxMessage is added to the SAME       │
/// │                  │  DbContext, it participates in the same SQL batch     │
/// │                  │  and database transaction. If the transaction rolls   │
/// │                  │  back, outbox records roll back with domain data.     │
/// ├──────────────────────────────────────────────────────────────────────────┤
/// │  AFTER COMMIT   │  DomainEventsInterceptor                              │
/// │  (SavedChanges)  │  → Publishes captured events via MediatR             │
/// │                  │  → THEN clears DomainEvents from aggregates          │
/// │                  │                                                       │
/// │                  │  Local handlers see a consistent, committed state.    │
/// │                  │  If a handler fails, the transaction is NOT rolled    │
/// │                  │  back (it already committed). Handlers must be        │
/// │                  │  idempotent.                                          │
/// └──────────────────────────────────────────────────────────────────────────┘
///
/// <b>Performance guarantees:</b>
///   ✓ ChangeTracker enumerated once via .Entries&lt;BaseEntity&gt;()
///   ✓ DomainEvents enumerated once per entity
///   ✓ Serialisation executed once per event
///   ✓ No reflection-based assembly scanning
///   ✓ Single static JsonSerializerOptions instance (no per-call allocation)
///
/// <b>Layer isolation:</b>
/// OutboxMessage is an Infrastructure-only entity. The Domain layer has no
/// reference to it. IAppDbContext intentionally does not expose it.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<OutboxSaveChangesInterceptor> _logger;

    public OutboxSaveChangesInterceptor(ILogger<OutboxSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void PersistOutboxMessages(DbContext? context)
    {
        if (context is null)
            return;

        if (context is not AppDbContext dbContext)
            return;

        var now = DateTime.UtcNow;

        // Enumerate ChangeTracker once, materialize immediately.
        var entries = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        var outboxMessages = new List<OutboxMessage>(entries.Length);
        var aggregateCount = 0;
        var totalEvents = 0;

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var events = entity.DomainEvents;

            if (events.Count == 0)
                continue;

            aggregateCount++;
            totalEvents += events.Count;

            // Serialise each event once.
            foreach (var domainEvent in events)
            {
                var payload = JsonSerializer.Serialize(domainEvent, OutboxConstants.SerializerOptions);

                if (string.IsNullOrEmpty(payload))
                {
                    throw new InvalidOperationException(
                        $"Serialization of domain event '{domainEvent.GetType().FullName}' produced an empty payload. " +
                        "The outbox message cannot be created.");
                }

                var eventType = domainEvent.GetType().FullName!;
                var assemblyName = domainEvent.GetType().Assembly.GetName().Name!;

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    AssemblyName = assemblyName,
                    EventVersion = OutboxConstants.EventVersion,
                    Payload = payload,
                    OccurredOnUtc = now
                };

                outboxMessages.Add(outboxMessage);
            }
        }

        if (outboxMessages.Count > 0)
        {
            dbContext.OutboxMessages.AddRange(outboxMessages);
            _logger.LogDebug(
                "Collected domain events from {AggregateCount} aggregate(s). " +
                "Persisted {EventCount} outbox message(s).",
                aggregateCount, totalEvents);
        }
    }
}
