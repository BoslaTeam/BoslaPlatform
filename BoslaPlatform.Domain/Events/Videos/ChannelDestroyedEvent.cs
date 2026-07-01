using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    /// <summary>
    /// Agora-specific integration event raised when an Agora channel is destroyed.
    /// 
    /// This is NOT a business domain event — it is an integration event that carries
    /// Agora-specific data (channel name, Agora timestamp) for monitoring, analytics,
    /// or cross-referencing with Agora dashboards. Business handlers should subscribe
    /// to <see cref="VideoSessionEndedEvent"/> instead, which is raised alongside
    /// this event.
    ///
    /// WHY IT EXISTS:
    ///   Agora fires a "channel_destroy" callback (eventType 111) when the LAST participant
    ///   leaves a channel. This event carries the Agora channel name and timestamp, which
    ///   are not available on the business event (VideoSessionEndedEvent). Handlers that
    ///   need these details (e.g., logging, monitoring, analytics) subscribe to this event.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Domain layer — this is a pure fact about what happened inside the VideoSession
    ///   aggregate. Raised before any infrastructure side-effects. Handlers (application
    ///   or infrastructure) implement INotificationHandler<ChannelDestroyedEvent> to react.
    ///
    /// HOW IT COMMUNICATES WITH THE DOMAIN:
    ///   Raised exclusively from VideoSession.ChannelDestroyed() aggregate method.
    ///   Never instantiated directly by application or infrastructure code.
    /// </summary>
    public sealed class ChannelDestroyedEvent : DomainEvent
    {
        /// <summary>
        /// The unique identifier of the video session whose channel was destroyed.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// The appointment ID associated with this session.
        /// Carried here so handlers can trigger appointment-level workflows
        /// (e.g., mark appointment as completed) without loading the session again.
        /// </summary>
        public Guid AppointmentId { get; }

        /// <summary>
        /// The Agora channel name that was destroyed.
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// The UTC timestamp when the channel destruction event was received from Agora.
        /// Derived from the Agora webhook payload's <c>ts</c> field.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChannelDestroyedEvent"/>.
        /// </summary>
        /// <param name="sessionId">The video session identifier.</param>
        /// <param name="appointmentId">The associated appointment identifier.</param>
        /// <param name="channelName">The Agora channel name.</param>
        /// <param name="occurredAtUtc">The timestamp from the Agora webhook payload.</param>
        public ChannelDestroyedEvent(
            Guid sessionId,
            Guid appointmentId,
            string channelName,
            DateTimeOffset occurredAtUtc)
        {
            SessionId = sessionId;
            AppointmentId = appointmentId;
            ChannelName = channelName;
            OccurredAtUtc = occurredAtUtc;
        }
    }
}
