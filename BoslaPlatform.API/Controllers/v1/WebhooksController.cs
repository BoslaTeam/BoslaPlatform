using System.Text;
using System.Text.Json;
using Asp.Versioning;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Infrastructure.Agora.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    /// <summary>
    /// Receives and processes incoming Agora Notification Service webhook callbacks.
    ///
    /// WHY IT EXISTS:
    ///   Agora sends server-to-server HTTP POST requests to notify our platform of
    ///   participant activity, channel lifecycle events, and recording status.
    ///   This controller is the ONLY entry point for these events.
    ///   The frontend MUST NEVER report participant activity directly — Agora is
    ///   the authoritative source of truth for all video session state changes.
    ///
    /// SECURITY:
    ///   The endpoint is [AllowAnonymous] because it is called by Agora's servers,
    ///   not by our authenticated users. JWT authentication is not applicable here.
    ///   Security is enforced exclusively by HMAC-SHA256 signature verification
    ///   (IAgoraWebhookSignatureVerifier). Any request that fails signature
    ///   verification is rejected with 401 Unauthorized before any business logic runs.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   API (Presentation) layer. This controller is deliberately thin:
    ///     1. Reads raw body bytes (for HMAC computation).
    ///     2. Verifies signature — rejects if invalid.
    ///     3. Deserializes payload.
    ///     4. Delegates ALL business logic to IVideoSessionWebhookService.
    ///     5. Returns 200 OK.
    ///   It contains zero domain or business logic itself.
    ///
    /// WHY ALWAYS RETURN 200 AFTER SIGNATURE VALIDATION:
    ///   Agora's retry policy: if it does not receive HTTP 200, it retries the
    ///   webhook up to N times with exponential backoff. If our business logic
    ///   fails (e.g., session not found), returning 500 would trigger a retry
    ///   storm. Instead, the service handles errors gracefully and logs them.
    ///   We only return non-200 for security rejections (401) and malformed
    ///   payloads (400) — events where a retry from Agora is also expected to fail.
    /// </summary>
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/webhooks")]
    [ApiController]
    [AllowAnonymous] // SECURITY: This endpoint is secured by HMAC, not JWT. See class doc.
    public class WebhooksController : ControllerBase
    {
        private readonly IAgoraWebhookSignatureVerifier _signatureVerifier;
        private readonly IVideoSessionWebhookService _webhookService;
        private readonly ILogger<WebhooksController> _logger;

        // The Agora webhook signature header name.
        // Agora V2 uses "Agora-Signature-V2". V1 used "Agora-Signature" (deprecated).
        private const string AgoraSignatureHeader = "Agora-Signature-V2";

        /// <summary>
        /// Initializes a new instance of <see cref="WebhooksController"/>.
        /// </summary>
        /// <param name="signatureVerifier">The Agora HMAC signature verifier.</param>
        /// <param name="webhookService">The webhook event processing service.</param>
        /// <param name="logger">Structured logger.</param>
        public WebhooksController(
            IAgoraWebhookSignatureVerifier signatureVerifier,
            IVideoSessionWebhookService webhookService,
            ILogger<WebhooksController> logger)
        {
            _signatureVerifier = signatureVerifier
                ?? throw new ArgumentNullException(nameof(signatureVerifier));
            _webhookService = webhookService
                ?? throw new ArgumentNullException(nameof(webhookService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Receives an Agora Notification Service webhook event.
        /// </summary>
        /// <remarks>
        /// This endpoint is the single entry point for all Agora video session events.
        ///
        /// Supported events:
        /// - eventType 103: User joined the channel
        /// - eventType 104: User left the channel
        /// - eventType 110: Channel created (first user joined)
        /// - eventType 111: Channel destroyed (last user left)
        ///
        /// Cloud Recording events (productId 3 — matched on productId + eventType):
        /// - eventType 4:  M3U8 playlist generated (media is being captured)
        /// - eventType 11: Session exit
        /// - eventType 31: Files uploaded to third-party storage
        /// - eventType 32: Files uploaded, at least one to Agora Cloud Backup
        /// - eventType 40: Recorder started
        /// - eventType 41: Recorder left the channel
        /// - Any other eventType: Gracefully ignored
        ///
        /// Security: Requests without a valid Agora-Signature-V2 header are rejected with 401.
        /// Requests with a timestamp outside the 5-minute window are rejected with 400.
        /// </remarks>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// 200 OK if the event was received and processed (or safely ignored).
        /// 400 Bad Request if the payload is malformed or the timestamp is outside the replay window.
        /// 401 Unauthorized if the HMAC signature is missing or invalid.
        /// </returns>
        /// <response code="200">Event received and processed successfully.</response>
        /// <response code="400">Malformed payload or timestamp outside replay window.</response>
        /// <response code="401">Invalid or missing HMAC signature.</response>
        [HttpPost("agora")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [EndpointName("AgoraWebhook")]
        [EndpointSummary("Agora Notification Service webhook receiver.")]
        [EndpointDescription("Receives Agora video session events and updates domain state. Secured via HMAC-SHA256 signature verification.")]
        [Tags("Webhooks")]
        public async Task<IResult> ReceiveAgoraEvent(CancellationToken ct)
        {
            // ------------------------------------------------------------------
            // Step 1: Read raw body bytes BEFORE any deserialization.
            //
            // CRITICAL: The HMAC is computed over the exact raw bytes that Agora
            // sent. We must read the body stream ourselves before the framework
            // can consume it. We enable buffering so the stream can be read twice
            // (once for HMAC, once for JSON deserialization).
            // ------------------------------------------------------------------
            Request.EnableBuffering();

            byte[] rawBody;
            try
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms, ct);
                rawBody = ms.ToArray();
                // Reset the position so the framework or our own code can re-read it.
                Request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AgoraWebhook] Failed to read request body.");
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Failed to read webhook payload.");
            }

            // ------------------------------------------------------------------
            // Step 2: Extract the signature header and deserialize enough of
            // the payload to get the timestamp (ts) for replay protection.
            //
            // We do a preliminary parse just for the timestamp so we can pass
            // it to the verifier. The full deserialization follows after.
            // ------------------------------------------------------------------
            var signatureHeader = Request.Headers[AgoraSignatureHeader].FirstOrDefault();

            long timestampSeconds = 0;
            AgoraWebhookRequest? webhookRequest = null;

            try
            {
                var bodyText = Encoding.UTF8.GetString(rawBody);

                webhookRequest = JsonSerializer.Deserialize<AgoraWebhookRequest>(
                    bodyText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                timestampSeconds = webhookRequest?.Payload.Ts ?? 0;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[AgoraWebhook] Failed to deserialize webhook payload. Rejecting.");
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Malformed webhook payload.");
            }

            if (webhookRequest is null)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] Deserialized payload was null. Rejecting.");
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Empty webhook payload.");
            }

            // ------------------------------------------------------------------
            // Step 3: HMAC-SHA256 signature verification.
            //
            // SECURITY GATE: No business logic executes if this fails.
            // The verifier also enforces the replay window internally.
            // ------------------------------------------------------------------
            // Capture the payload verbatim BEFORE routing. Agora's event codes and
            // payload shape are the source of truth for our routing logic, and
            // guessing them from documentation has already produced one production
            // bug. The raw body is the only artifact that settles it.
            // ------------------------------------------------------------------
            _logger.LogInformation(
                "[AgoraWebhook] RAW | ProductId={ProductId} | EventType={EventType} | NoticeId={NoticeId} | " +
                "Headers={Headers} | Body={Body}",
                webhookRequest.ProductId,
                webhookRequest.EventType,
                webhookRequest.NoticeId,
                string.Join(",", Request.Headers.Keys),
                Encoding.UTF8.GetString(rawBody));

            _logger.LogInformation(
                "[AgoraWebhook] Verifying signature | NoticeId={NoticeId} | EventType={EventType} | Ts={Ts}",
                webhookRequest.NoticeId,
                webhookRequest.EventType,
                timestampSeconds);

            var isSignatureValid = _signatureVerifier.Verify(
                rawBody,
                signatureHeader,
                timestampSeconds);

            if (!isSignatureValid)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] Signature verification FAILED | NoticeId={NoticeId} | " +
                    "EventType={EventType} | IP={RemoteIp}",
                    webhookRequest.NoticeId,
                    webhookRequest.EventType,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Webhook signature verification failed.");
            }

            _logger.LogInformation(
                "[AgoraWebhook] Signature verified | NoticeId={NoticeId}",
                webhookRequest.NoticeId);

            // ------------------------------------------------------------------
            // Step 4: Delegate processing to the application service.
            //
            // IMPORTANT: We always return 200 OK after this point, even if the
            // service encounters a non-security error (e.g., session not found).
            // This prevents Agora from retrying events that our service cannot
            // process (which would create a retry storm against our database).
            // All errors are logged by the service itself.
            // ------------------------------------------------------------------
            var result = await _webhookService.ProcessAsync(webhookRequest, ct);

            if (result.IsError)
            {
                // This is a true domain validation error (e.g., session in invalid state).
                // We log it but still return 200 to prevent Agora retries.
                _logger.LogWarning(
                    "[AgoraWebhook] Processing completed with domain error | NoticeId={NoticeId} | Error={Error}",
                    webhookRequest.NoticeId,
                    result.Errors[0].Description);
            }

            // Always return 200 after signature validation.
            return Results.Ok(new
            {
                received = true,
                noticeId = webhookRequest.NoticeId
            });
        }
    }
}
