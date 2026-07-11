namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Resolves a CLR <see cref="Type"/> from the outbox metadata (<see cref="OutboxMessage.AssemblyName"/>
/// and <see cref="OutboxMessage.EventType"/>), caching results to avoid repeated <c>Assembly.Load</c>
/// calls for the same type.
///
/// Implementations must be thread-safe Singleton.
/// </summary>
public interface IEventTypeResolver
{
    /// <summary>
    /// Resolves the CLR type for the given assembly and event type names.
    /// </summary>
    /// <param name="assemblyName">Simple assembly name (e.g., "BoslaPlatform.Domain").</param>
    /// <param name="eventType">Full CLR type name (e.g., "BoslaPlatform.Domain.Events.Apoointments.AppointmentCompletedEvent").</param>
    /// <returns>The resolved <see cref="Type"/>.</returns>
    /// <exception cref="InvalidOperationException">If the type cannot be resolved.</exception>
    Type Resolve(string assemblyName, string eventType);
}
