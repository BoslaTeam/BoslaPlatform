namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Contains constant policy names for rate limiting policies defined in the platform.
/// Each policy targets a specific traffic profile with distinct limits and partition behavior.
/// </summary>
public static class RateLimitPolicyNames
{
    /// <summary>
    /// Applied to unauthenticated (anonymous) requests to public endpoints.
    /// Typically the most restrictive policy to prevent abuse from unknown sources.
    /// Partitioned by client IP address.
    /// </summary>
    public const string Anonymous = "Anonymous";

    /// <summary>
    /// Applied to authenticated user requests for standard API endpoints.
    /// Partitioned by the authenticated user's unique identifier.
    /// </summary>
    public const string Authenticated = "Authenticated";

    /// <summary>
    /// Applied to sensitive operations such as password changes, email updates, or account deletion.
    /// More restrictive than <see cref="Authenticated"/> due to the higher risk profile.
    /// </summary>
    public const string Sensitive = "Sensitive";

    /// <summary>
    /// Applied to file upload endpoints (profile images, documents, etc.).
    /// Lower permit limit reflecting the higher computational and storage cost per upload.
    /// </summary>
    public const string Upload = "Upload";

    /// <summary>
    /// Applied to AI-powered features (chat, summarization, analysis).
    /// Strictly limited due to high inference costs and external API rate dependencies.
    /// </summary>
    public const string AI = "AI";

    /// <summary>
    /// Applied to public search and discovery endpoints that do not require authentication.
    /// Allows reasonable browsing while preventing scraping or excessive query volume.
    /// </summary>
    public const string PublicSearch = "PublicSearch";
}
