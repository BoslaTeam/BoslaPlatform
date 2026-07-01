using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    /// <summary>
    /// Agora-specific integration event raised when an Agora channel is created.
    /// 
    /// This is NOT a business domain event — it is an integration event that carries
    /// Agora-specific data (channel name, Agora timestamp) for monitoring, analytics,
    /// or cross-referencing with Agora dashboards. Business handlers should subscribe
    /// to <see cref="VideoSessionStartedEvent"/> instead, which is raised alongside
    /// this event.
    /// 
    /// WHY IT EXISTS:
    ///   Agora fires a "channel_create" callback (eventType 110) when the FIRST participant
    ///   joins a channel. This event carries the Agora channel name and timestamp, which
    ///   are not available on the business event (VideoSessionStartedEvent). Handlers that
    ///   need these details (e.g., logging, monitoring, analytics) subscribe to this event.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Domain layer — this is a pure fact about what happened inside the VideoSession
    ///   aggregate. No infrastructure concerns here. MediatR dispatch is handled by the
    ///   DomainEventsInterceptor in the Infrastructure layer after SaveChanges.
    ///
    /// HOW IT COMMUNICATES WITH THE DOMAIN:
    ///   Raised exclusively from VideoSession.ChannelCreated() aggregate method.
    ///   Never instantiated directly by application or infrastructure code.
    /// </summary>
    public sealed class ChannelCreatedEvent : DomainEvent
    {
        /// <summary>
        /// The unique identifier of the video session whose channel was created.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// The Agora channel name that was created.
        /// Useful for handlers that need to correlate with external systems.
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// The UTC timestamp when the channel creation event was received from Agora.
        /// Derived from the Agora webhook payload's <c>ts</c> field.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChannelCreatedEvent"/>.
        /// </summary>
        /// <param name="sessionId">The video session identifier.</param>
        /// <param name="channelName">The Agora channel name.</param>
        /// <param name="occurredAtUtc">The timestamp from the Agora webhook payload.</param>
        public ChannelCreatedEvent(
            Guid sessionId,
            string channelName,
            DateTimeOffset occurredAtUtc)
        {
            SessionId = sessionId;
            ChannelName = channelName;
            OccurredAtUtc = occurredAtUtc;
        }
    }
}
