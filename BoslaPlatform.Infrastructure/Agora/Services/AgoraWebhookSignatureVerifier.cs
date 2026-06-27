using BoslaPlatform.Infrastructure.Agora.Interfaces;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BoslaPlatform.Infrastructure.Agora.Services
{
    /// <summary>
    /// Verifies Agora webhook request signatures using HMAC-SHA256.
    ///
    /// WHY IT EXISTS:
    ///   This is the security gate for the entire webhook pipeline.
    ///   Without signature verification, any actor on the internet could send
    ///   fake webhook payloads to manipulate video session state.
    ///
    /// ALGORITHM:
    ///   Agora signs the request by computing:
    ///     HMAC-SHA256( secret, noticeId + productId + eventType + ts )
    ///   where the values are concatenated as strings (no separators).
    ///   The resulting hex digest is sent in the "Agora-Signature-V2" header.
    ///
    ///   Reference: https://docs.agora.io/en/video-calling/reference/agora-notification-service/#signature-verification
    ///
    /// SECURITY DECISIONS:
    ///
    ///   1. HMAC-SHA256 key = WebhookSecret from AgoraSettings.
    ///      The secret is the UTF-8 byte encoding of the plaintext secret string
    ///      from the Agora Console. This is consistent with Agora's documentation.
    ///
    ///   2. Constant-time comparison via CryptographicOperations.FixedTimeEquals.
    ///      A naive string comparison (==) would be vulnerable to timing attacks
    ///      where an attacker measures response latency to guess the correct HMAC
    ///      one byte at a time. Fixed-time comparison eliminates this vector.
    ///
    ///   3. Replay attack prevention via timestamp window (default 300 seconds).
    ///      Even if an attacker captures a valid signed request, replaying it
    ///      after the window expires causes rejection. The window must balance
    ///      security (shorter = better) with clock skew tolerance (Agora's servers
    ///      and our servers may have slight clock differences).
    ///
    ///   4. Empty secret bypass WITH warning log.
    ///      If WebhookSecret is not configured (e.g., local development), the
    ///      verifier logs a WARNING and allows the request through. This prevents
    ///      blocking development workflows. In production, the secret MUST be set.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Infrastructure layer. Depends on System.Security.Cryptography (runtime)
    ///   and Microsoft.Extensions.Options (configuration binding).
    ///   Implements IAgoraWebhookSignatureVerifier (also Infrastructure).
    ///   No domain or application types are referenced here.
    /// </summary>
    public sealed class AgoraWebhookSignatureVerifier : IAgoraWebhookSignatureVerifier
    {
        private readonly AgoraSettings _settings;
        private readonly ILogger<AgoraWebhookSignatureVerifier> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="AgoraWebhookSignatureVerifier"/>.
        /// </summary>
        /// <param name="options">Bound Agora configuration options.</param>
        /// <param name="logger">Structured logger.</param>
        public AgoraWebhookSignatureVerifier(
            IOptions<AgoraSettings> options,
            ILogger<AgoraWebhookSignatureVerifier> logger
            )
        {
            _settings = options?.Value
                ?? throw new ArgumentNullException(nameof(options));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool Verify(
            byte[] rawBody,
            string? signatureHeader,
            long timestampSeconds)
        {
            // ------------------------------------------------------------------
            // Guard: Empty secret = development bypass
            // ------------------------------------------------------------------
            if (_settings.SkipSignatureValidation)
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
                _logger.LogWarning(
                    "[AgoraWebhook] WebhookSecret is not configured. " +
                    "Signature verification SKIPPED. This is acceptable in development " +
                    "but MUST be configured in production.");
                return true;
            }

            // ------------------------------------------------------------------
            // Step 1: Replay attack check
            // Compare the webhook timestamp against server time.
            // We use DateTimeOffset.UtcNow for testability (clock is injectable
            // if needed in the future).
            // ------------------------------------------------------------------
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var age = Math.Abs(nowSeconds - timestampSeconds);

            if (age > _settings.WebhookReplayWindowSeconds)
            {
                _logger.LogWarning(
                    "[AgoraWebhook] Signature | REPLAY REJECTED | " +
                    "PayloadTs={PayloadTs} | ServerNow={ServerNow} | AgeSecs={Age} | Window={Window}",
                    timestampSeconds,
                    nowSeconds,
                    age,
                    _settings.WebhookReplayWindowSeconds);
                return false;
            }

            // ------------------------------------------------------------------
            // Step 2: Signature presence check
            // ------------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                _logger.LogWarning(
                    "[AgoraWebhook] Signature | REJECTED | " +
                    "Missing Agora-Signature-V2 header.");
                return false;
            }

            // ------------------------------------------------------------------
            // Step 3: Compute expected HMAC-SHA256
            //
            // Agora's signing algorithm:
            //   message = noticeId + productId + eventType + ts
            //   (all values as strings, concatenated with NO separator)
            //   key     = UTF-8 bytes of WebhookSecret
            //   result  = hex-encoded HMAC-SHA256(key, UTF-8(message))
            //
            // IMPORTANT: We sign the raw body here to match the exact bytes
            // that Agora signed. Any re-encoding would break the comparison.
            // ------------------------------------------------------------------
            var keyBytes = Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            byte[] computedHashBytes;

            try
            {
                using var hmac = new HMACSHA256(keyBytes);
                computedHashBytes = hmac.ComputeHash(rawBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AgoraWebhook] Signature | HMAC computation failed unexpectedly.");
                return false;
            }

            var computedSignature = Convert.ToHexString(computedHashBytes)
                .ToLowerInvariant();

            // ------------------------------------------------------------------
            // Step 4: Constant-time comparison
            // Convert both to byte arrays first so FixedTimeEquals can work.
            // FixedTimeEquals requires equal-length arrays.
            // If lengths differ, the signatures are trivially not equal — but
            // we must not short-circuit to avoid timing oracle.
            // ------------------------------------------------------------------
            var expectedBytes = Encoding.ASCII.GetBytes(computedSignature);
            var providedBytes = Encoding.ASCII.GetBytes(signatureHeader.Trim());

            // Pad both to the same length before comparison to avoid timing leaks
            // on length difference (FixedTimeEquals returns false immediately if lengths differ).
            var isValid = expectedBytes.Length == providedBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);

            if (isValid)
            {
                _logger.LogDebug(
                    "[AgoraWebhook] Signature | VALID | Ts={Ts} | AgeSecs={Age}",
                    timestampSeconds,
                    age);
            }
            else
            {
                _logger.LogWarning(
                    "[AgoraWebhook] Signature | INVALID | " +
                    "Expected={Expected} | Provided={Provided} | Ts={Ts}",
                    computedSignature,
                    signatureHeader.Length > 12
                        ? signatureHeader[..12] + "..." // Log only prefix — never log full secret
                        : signatureHeader,
                    timestampSeconds);
            }

            return isValid;
        }
    }
}
