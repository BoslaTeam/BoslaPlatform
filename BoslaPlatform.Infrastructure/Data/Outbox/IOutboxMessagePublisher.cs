namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Abstraction for publishing a deserialised outbox message to its ultimate destination.
///
/// The implementation determines the actual delivery mechanism:
///   - Current: <see cref="NoOpOutboxMessagePublisher"/> — deserialises, logs, returns success.
///   - Future:  RabbitMQ, Kafka, Azure Service Bus, HTTP, etc.
///
/// This interface lives in Infrastructure (not Domain, not Application) because
/// the outbox is an Infrastructure-only concern.
/// </summary>
public interface IOutboxMessagePublisher
{
    /// <summary>
    /// Publishes the deserialised domain event.
    /// </summary>
    /// <param name="message">The original outbox message (metadata available to the publisher).</param>
    /// <param name="deserializedEvent">The deserialised domain event object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAsync(OutboxMessage message, object deserializedEvent, CancellationToken cancellationToken = default);
}
