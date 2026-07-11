namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Represents a message to be published to an outbox for eventual delivery to a message broker.
/// This entity lives exclusively in the Infrastructure layer and must not be referenced by the Domain or Application layers.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for this outbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full CLR type name of the event (e.g., "UserRegisteredEvent").
    /// Used by the dispatcher to determine how to deserialize and route the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized JSON payload of the event.
    /// This is the data that will be published to the message broker.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when this message was created.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when this message was successfully processed and published.
    /// A null value means the message has not yet been processed.
    /// This field is nullable because the message may not have been dispatched yet.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the last error that occurred during processing, if any.
    /// A null value means no error has been recorded, or the last attempt succeeded.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the last processing attempt.
    /// Used for retry scheduling and back-off calculations.
    /// A null value means no processing attempt has been made yet.
    /// </summary>
    public DateTime? LastAttemptUtc { get; set; }

    /// <summary>
    /// Gets or sets the number of times processing has been attempted for this message.
    /// Starts at 0 and increments on each attempt.
    /// </summary>
    public int RetryCount { get; set; }
}
