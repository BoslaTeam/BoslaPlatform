using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.VideoSessions.Interfaces
{
    /// <summary>
    /// Application service interface for processing Agora webhook notifications.
    ///
    /// WHY IT EXISTS:
    ///   Separating the webhook processing contract from the general video session
    ///   service (IVideoSessionService) follows the Interface Segregation Principle.
    ///   Webhook processing has distinct concerns: it does not require an authenticated
    ///   user context, it receives push data from an external system, and it must be
    ///   extremely robust to unknown or malformed inputs. A dedicated interface makes
    ///   these contracts explicit and testable in isolation.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Application layer — this interface defines the port. The implementation
    ///   (VideoSessionWebhookService) is also in the Application layer because the
    ///   logic is pure business orchestration: look up an aggregate, call a method,
    ///   save changes. No infrastructure concerns are present in the implementation.
    ///
    /// HOW IT COMMUNICATES WITH THE DOMAIN:
    ///   The implementation resolves VideoSession aggregates from IAppDbContext,
    ///   calls domain aggregate methods (e.g., ParticipantJoined, RecordingStarted),
    ///   and then calls SaveChangesAsync. The DomainEventsInterceptor then dispatches
    ///   all raised domain events via MediatR automatically.
    /// </summary>
    public interface IVideoSessionWebhookService
    {
        /// <summary>
        /// Processes an incoming Agora webhook notification.
        ///
        /// CONTRACT:
        ///   - This method MUST NOT throw exceptions. All errors are returned via Result.
        ///   - Unknown event types MUST be handled gracefully (logged and ignored).
        ///   - If the VideoSession is not found, the method returns success (idempotent).
        ///     This prevents Agora's retry mechanism from flooding the system for stale events.
        ///   - The method supports CancellationToken for cooperative cancellation.
        /// </summary>
        /// <param name="request">
        ///   The deserialized Agora webhook payload. The controller is responsible for
        ///   signature verification before calling this method.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        ///   Result.Success if the event was processed or safely ignored.
        ///   Result.Failure only for true domain validation errors (e.g., session in invalid state).
        /// </returns>
        Task<Result> ProcessAsync(
            AgoraWebhookRequest request,
            CancellationToken ct = default);
    }
}
