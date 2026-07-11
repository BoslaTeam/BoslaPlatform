using System.Text.Json;

namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Centralised constants for the Outbox infrastructure.
///
/// All magic values (max lengths, default versions, serializer configuration)
/// are defined here so they can be referenced from the entity, configuration,
/// interceptor, and future dispatcher without duplication.
/// </summary>
public static class OutboxConstants
{
    /// <summary>
    /// Current version number assigned to every outbound outbox message.
    /// Reserved for future schema migration and version-aware dispatching.
    /// The application always populates this value explicitly via code —
    /// there is no SQL-level default constraint because the C# default
    /// initialiser on <see cref="OutboxMessage.EventVersion"/> is the
    /// single source of truth.
    /// </summary>
    public const int EventVersion = 1;

    /// <summary>
    /// Maximum length of the <see cref="OutboxMessage.EventType"/> column.
    /// 500 characters accommodates fully-qualified CLR type names
    /// (e.g., "BoslaPlatform.Domain.Events.Apoointments.AppointmentCompletedEvent").
    /// </summary>
    public const int EventTypeMaxLength = 500;

    /// <summary>
    /// Maximum length of the <see cref="OutboxMessage.AssemblyName"/> column.
    /// 500 characters comfortably holds assembly names such as
    /// "BoslaPlatform.Domain" or "BoslaPlatform.Infrastructure".
    /// </summary>
    public const int AssemblyNameMaxLength = 500;

    /// <summary>
    /// Maximum length of the <see cref="OutboxMessage.LastError"/> column.
    /// 2000 characters is sufficient for:
    ///   - Full .NET exception stack traces (typically 500-1500 chars)
    ///   - Serialised error details from message broker rejections
    ///   - Diagnostic messages with correlation identifiers
    /// This limit keeps the column size manageable in SQL Server (nvarchar(2000))
    /// while allowing operators to triage the vast majority of failure modes
    /// without needing to consult external logs. Messages exceeding 2000 chars
    /// are truncated at the boundary; full details should always be captured
    /// in structured application logs.
    /// </summary>
    public const int ErrorMaxLength = 2000;

    /// <summary>
    /// Shared, reusable <see cref="JsonSerializerOptions"/> instance for serializing
    /// domain events into outbox payloads.
    ///
    /// Uses default settings (camelCase, property naming consistent with
    /// the rest of the platform). A single static instance avoids allocating
    /// new options on every SaveChanges.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new();
}
