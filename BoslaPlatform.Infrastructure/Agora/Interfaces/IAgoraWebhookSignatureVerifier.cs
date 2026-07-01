namespace BoslaPlatform.Infrastructure.Agora.Interfaces
{
    /// <summary>
    /// Verifies the authenticity and freshness of incoming Agora webhook requests.
    ///
    /// WHY IT EXISTS:
    ///   This interface separates the security verification concern from the business
    ///   logic concern. The controller uses it as a gate — no processing happens
    ///   until the signature is verified. Abstracting it behind an interface allows
    ///   the implementation to be replaced for testing (e.g., a no-op verifier for
    ///   development) without changing the controller.
    ///
    /// SECURITY MODEL:
    ///   Agora signs every webhook request using HMAC-SHA256.
    ///   The signature is sent in the "Agora-Signature-V2" HTTP header.
    ///   The secret is configured in the Agora Console (Notifications page).
    ///
    ///   Two attacks are mitigated:
    ///   1. Forgery  — A request without a valid HMAC cannot pass verification.
    ///   2. Replay   — A valid captured request replayed after the time window is rejected.
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Infrastructure layer — this is an infrastructure concern (cryptography,
    ///   HTTP header parsing). The Application and Domain layers do not know about it.
    /// </summary>
    public interface IAgoraWebhookSignatureVerifier
    {
        /// <summary>
        /// Verifies that the incoming webhook request is authentic and fresh.
        /// </summary>
        /// <param name="rawBody">
        ///   The raw, unmodified request body bytes.
        ///   IMPORTANT: This must be read BEFORE the model binder consumes the stream,
        ///   which is why the controller enables buffering and reads the body manually.
        /// </param>
        /// <param name="signatureHeader">
        ///   The value of the "Agora-Signature-V2" HTTP header sent by Agora.
        ///   May be null or empty if Agora did not include the header
        ///   (which itself is treated as a verification failure).
        /// </param>
        /// <param name="timestampSeconds">
        ///   The Unix timestamp (seconds since epoch) extracted from the webhook payload's
        ///   <c>ts</c> field. Used for replay attack prevention.
        /// </param>
        /// <returns>
        ///   <c>true</c> if the signature is valid and the timestamp is within the
        ///   configured replay window; <c>false</c> otherwise.
        /// </returns>
        bool Verify(
            byte[] rawBody,
            string? signatureHeader,
            long timestampSeconds);
    }
}
