using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// A no-op implementation of <see cref="IOutboxMessagePublisher"/> that deserialises
/// the event payload and logs a debug message without delivering it anywhere.
///
/// This is the initial implementation used while the outbox infrastructure is being
/// built. Once a real message broker is introduced (RabbitMQ, Kafka, etc.), this
/// implementation will be replaced.
///
/// <b>Current behaviour:</b>
///   ✓ Deserialises the payload using <see cref="OutboxConstants.SerializerOptions"/>
///   ✓ Logs a Debug message with the event type and message ID
///   ✓ Returns immediately (no external communication)
/// </summary>
public sealed class NoOpOutboxMessagePublisher : IOutboxMessagePublisher
{
    private readonly ILogger<NoOpOutboxMessagePublisher> _logger;

    public NoOpOutboxMessagePublisher(ILogger<NoOpOutboxMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(OutboxMessage message, object deserializedEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Publishing event {EventType} with Id {MessageId}",
            message.EventType, message.Id);

        return Task.CompletedTask;
    }
}
