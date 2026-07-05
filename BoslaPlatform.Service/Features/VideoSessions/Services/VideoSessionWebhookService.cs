using BoslaPlatform.Application.Features.VideoSessions.Constants;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Application.Features.VideoSessions.Services
{
    /// <summary>
    /// Application service that processes Agora webhook notifications.
    ///
    /// WHY IT EXISTS:
    ///   This service is the single entry point for all Agora webhook events.
    ///   It bridges the infrastructure boundary (raw HTTP webhook payload) and the
    ///   domain (VideoSession aggregate). By keeping it in the Application layer,
    ///   the mapping logic is testable without spinning up a web server.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Application layer. Depends on:
    ///     - IAppDbContext (Application abstraction, implemented by Infrastructure)
    ///     - ILogger (Microsoft.Extensions.Logging abstraction)
    ///     - VideoSession aggregate (Domain)
    ///   Does NOT depend on any Infrastructure types directly.
    ///
    /// HOW IT COMMUNICATES WITH THE DOMAIN:
    ///   1. Resolves the VideoSession aggregate by AgoraChannelName.
    ///   2. Calls the appropriate aggregate method (e.g., ParticipantJoined).
    ///   3. Calls SaveChangesAsync — the DomainEventsInterceptor fires and publishes
    ///      all accumulated domain events via MediatR automatically.
    ///
    /// IDEMPOTENCY STRATEGY:
    ///   If the session is not found, we return success and log a warning.
    ///   This prevents Agora's retry mechanism from creating noise in the system
    ///   for events that belong to sessions we do not recognize (e.g., test channels,
    ///   channels from a previous deployment).
    /// </summary>
    public sealed class VideoSessionWebhookService : IVideoSessionWebhookService
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<VideoSessionWebhookService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="VideoSessionWebhookService"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="logger">The structured logger.</param>
        public VideoSessionWebhookService(
            IAppDbContext context,
            ILogger<VideoSessionWebhookService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result> ProcessAsync(
            AgoraWebhookRequest request,
            CancellationToken ct = default)
        {
            // ------------------------------------------------------------------
            // Step 1: Structured log entry for every webhook that enters the system.
            // Every event is logged BEFORE any processing so we have a complete
            // audit trail regardless of what happens next.
            // ------------------------------------------------------------------
            _logger.LogInformation(
                "[AgoraWebhook] Received | NoticeId={NoticeId} | ProductId={ProductId} | EventType={EventType} | Channel={Channel} | Uid={Uid}",
                request.NoticeId,
                request.ProductId,
                request.EventType,
                request.Payload.ChannelName,
                request.Payload.Uid);

            // ------------------------------------------------------------------
            // Step 2: Route to the correct handler based on EventType.
            // Unknown event types are ignored gracefully — we never throw.
            // ------------------------------------------------------------------
            return request.EventType switch
            {
                AgoraEventTypes.UserJoined
                    => await HandleUserJoinedAsync(request, ct),

                AgoraEventTypes.UserLeft
                    => await HandleUserLeftAsync(request, ct),

                AgoraEventTypes.ChannelCreated
                    => await HandleChannelCreatedAsync(request, ct),

                AgoraEventTypes.ChannelDestroyed
                    => await HandleChannelDestroyedAsync(request, ct),

                AgoraEventTypes.RecordingStarted
                    => await HandleRecordingStartedAsync(request, ct),

                AgoraEventTypes.RecordingStopped
                or AgoraEventTypes.RecordingUploaded
                    => await HandleRecordingStoppedAsync(request, ct),

                // Any event type not in the list above is silently ignored.
                // This is intentional — Agora may add new event types at any time.
                // We log it so we can add support for it later if needed.
                _ => HandleUnknownEvent(request)
            };
        }

        // ------------------------------------------------------------------
        // Private handler methods — one per supported event type.
        // Each method follows the same pattern:
        //   1. Resolve session
        //   2. Call aggregate method
        //   3. SaveChangesAsync (domain events dispatched by interceptor)
        //   4. Log outcome
        // ------------------------------------------------------------------

        private async Task<Result> HandleUserJoinedAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.UserJoined);
            }

            var agoraUid = request.Payload.Uid;

            // Try to resolve the UserId from an existing participant record.
            // The record was created at token-generation time in AgoraTokenService.
            var existingParticipant = session.Participants
                .FirstOrDefault(p => p.AgoraUid == agoraUid);

            var resolvedUserId = existingParticipant?.UserId ?? Guid.Empty;

            if (resolvedUserId == Guid.Empty)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] UserJoined | No participant record found for AgoraUid={AgoraUid} in Channel={Channel}. " +
                    "Raising event with empty UserId.",
                    agoraUid,
                    request.Payload.ChannelName);
            }

            var result = session.ParticipantJoined(agoraUid, resolvedUserId);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] UserJoined | Aggregate rejected: {Error} | Channel={Channel} | AgoraUid={AgoraUid}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName,
                    agoraUid);
                return result;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] UserJoined | Processed | Channel={Channel} | AgoraUid={AgoraUid} | UserId={UserId} | DomainEventRaised=ParticipantJoinedVideoSessionEvent",
                request.Payload.ChannelName,
                agoraUid,
                resolvedUserId);

            return Result.Success();
        }

        private async Task<Result> HandleUserLeftAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.UserLeft);
            }

            var agoraUid = request.Payload.Uid;
            var result = session.ParticipantLeft(agoraUid);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] UserLeft | Aggregate rejected: {Error} | Channel={Channel} | AgoraUid={AgoraUid}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName,
                    agoraUid);
                return result;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] UserLeft | Processed | Channel={Channel} | AgoraUid={AgoraUid} | DomainEventRaised=ParticipantLeftVideoSessionEvent",
                request.Payload.ChannelName,
                agoraUid);

            return Result.Success();
        }

        private async Task<Result> HandleChannelCreatedAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.ChannelCreated);
            }

            var occurredAt = DateTimeOffset.FromUnixTimeSeconds(request.Payload.Ts);
            var result = session.ChannelCreated(request.Payload.ChannelName, occurredAt);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] ChannelCreated | Aggregate rejected: {Error} | Channel={Channel}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName);
                return result;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] ChannelCreated | Processed | Channel={Channel} | SessionId={SessionId} | " +
                "DomainEventsRaised=VideoSessionStartedEvent,ChannelCreatedEvent | " +
                "Note=ChannelCreated is the sole activator — Start() is validation only.",
                request.Payload.ChannelName,
                session.Id);

            return Result.Success();
        }

        private async Task<Result> HandleChannelDestroyedAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.ChannelDestroyed);
            }

            var occurredAt = DateTimeOffset.FromUnixTimeSeconds(request.Payload.Ts);
            var result = session.ChannelDestroyed(request.Payload.ChannelName, occurredAt);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] ChannelDestroyed | Aggregate rejected: {Error} | Channel={Channel}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName);
                return result;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] ChannelDestroyed | Processed | Channel={Channel} | SessionId={SessionId} | RecordingStatus={RecordingStatus} | DomainEventsRaised=VideoSessionEndedEvent,ChannelDestroyedEvent",
                request.Payload.ChannelName,
                session.Id,
                session.RecordingStatus);

            return Result.Success();
        }

        private async Task<Result> HandleRecordingStartedAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.RecordingStarted);
            }

            var details = request.Payload.Details;
            var resourceId = details?.ResourceId ?? string.Empty;
            var sid = details?.Sid ?? request.Payload.Sid ?? string.Empty;

            var result = session.ConfirmRecordingStarted(resourceId, sid);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] RecordingStarted | Aggregate rejected: {Error} | Channel={Channel}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName);
                return result;
            }

            if (session.CurrentRecording is not null)
            {
                session.CurrentRecording.AttachAgoraRecording(resourceId, sid);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] RecordingStarted | Confirmed | Channel={Channel} | ResourceId={ResourceId} | Sid={Sid} | RecordingStatus={Status}",
                request.Payload.ChannelName,
                resourceId,
                sid,
                session.RecordingStatus);

            return Result.Success();
        }

        private async Task<Result> HandleRecordingStoppedAsync(
            AgoraWebhookRequest request,
            CancellationToken ct)
        {
            var session = await ResolveSessionAsync(
                request.Payload.ChannelName,
                ct);

            if (session is null)
            {
                return LogSessionNotFound(
                    request.Payload.ChannelName,
                    AgoraEventTypes.RecordingStopped);
            }

            var details = request.Payload.Details;
            var fileUrl = details?.FileUrl;
            var duration = details?.Duration;
            var fileSize = details?.FileSize;

            var result = session.ConfirmRecordingStopped(fileUrl, duration, fileSize);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] RecordingStopped | Aggregate rejected: {Error} | Channel={Channel}",
                    result.Errors[0].Description,
                    request.Payload.ChannelName);
                return result;
            }

            var recording = session.CurrentRecording
                ?? session.Recordings
                    .OrderByDescending(recording => recording.CreatedAtUtc)
                    .FirstOrDefault();

            if (recording is not null
                && fileUrl is not null
                && recording.Status != BoslaPlatform.Domain.Enums.RecordingStatus.Failed)
            {
                recording.Complete(
                    fileUrl,
                    fileSize,
                    duration);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AgoraWebhook] RecordingStopped | Confirmed | Channel={Channel} | FileUrl={FileUrl} | Duration={Duration}s | RecordingStatus={Status}",
                request.Payload.ChannelName,
                fileUrl ?? "(none)",
                duration,
                session.RecordingStatus);

            return Result.Success();
        }

        private Result HandleUnknownEvent(AgoraWebhookRequest request)
        {
            // Not an error — Agora may add new event types.
            // We log it and return success so Agora does not retry.
            _logger.LogInformation(
                "[AgoraWebhook] Ignored | Unsupported EventType={EventType} | NoticeId={NoticeId} | Channel={Channel}",
                request.EventType,
                request.NoticeId,
                request.Payload.ChannelName);

            return Result.Success();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Loads the VideoSession aggregate from the database by Agora channel name.
        /// Eagerly loads Participants, CurrentRecording, and Recordings so aggregate methods
        /// can inspect them. Returns null if not found (caller decides how to handle).
        /// </summary>
        private async Task<Domain.Models.Video.VideoSession?> ResolveSessionAsync(
            string channelName,
            CancellationToken ct)
        {
            return await _context.VideoSessions
                .Include(vs => vs.Participants)
                .Include(vs => vs.CurrentRecording)
                .Include(vs => vs.Recordings)
                .FirstOrDefaultAsync(
                    vs => vs.AgoraChannelName == channelName,
                    ct);
        }

        /// <summary>
        /// Logs a warning for a session that cannot be found and returns success.
        /// Returning success (not an error) is intentional: if we returned an error,
        /// the controller would return a non-200 status, causing Agora to retry
        /// the same event repeatedly for a channel we will never recognize.
        /// </summary>
        private Result LogSessionNotFound(string channelName, int eventType)
        {
            _logger.LogWarning(
                "[AgoraWebhook] Session not found | Channel={Channel} | EventType={EventType} | Ignoring (idempotent).",
                channelName,
                eventType);

            return Result.Success();
        }
    }
}
