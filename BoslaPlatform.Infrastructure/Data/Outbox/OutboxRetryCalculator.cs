namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Provides the exponential back-off delay calculation for Outbox message retries.
///
/// <b>Why exponential back-off?</b>
/// Transient failures (network blips, service restarts, throttling) are often
/// resolved within seconds. A small initial delay avoids hammering the downstream
/// system. Exponential growth ensures that persistent failures do not cause
/// infinite busy-retry loops, while the cap guarantees the message is revisited
/// at least every <see cref="OutboxRetryOptions.MaxDelayMinutes"/>.
///
/// <b>Formula:</b>
///   delay = min(BaseDelaySeconds × 2^(retryCount − 1), MaxDelayMinutes × 60)
///
///   Example (BaseDelay = 30s, MaxDelay = 30 min = 1800s):
///     retryCount = 1 → 30 × 2^0  =  30s
///     retryCount = 2 → 30 × 2^1  =  60s
///     retryCount = 3 → 30 × 2^2  = 120s
///     retryCount = 4 → 30 × 2^3  = 240s
///     retryCount = 5 → 30 × 2^4  = 480s  (capped at 1800s)
///
/// <b>Why a dedicated class?</b>
/// Separating the arithmetic from <see cref="BackgroundJobs.OutboxDispatcherService"/>
/// makes the mathematics independently testable and keeps the dispatcher focused
/// on orchestration. No infrastructure dependencies (EF Core, logging, DI) are
/// needed, so the calculator is trivially unit-testable.
/// </summary>
public static class OutboxRetryCalculator
{
    /// <summary>
    /// Calculates the delay in seconds before the next retry attempt.
    /// </summary>
    /// <param name="retryCount">
    /// The number of retry attempts already performed (≥ 1).
    /// Pass the incremented count <em>after</em> recording a failure.
    /// </param>
    /// <param name="options">The retry policy options.</param>
    /// <returns>The delay in seconds for the next retry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount"/> is less than 1.
    /// </exception>
    public static double CalculateDelay(int retryCount, OutboxRetryOptions options)
    {
        if (retryCount < 1)
            throw new ArgumentOutOfRangeException(nameof(retryCount),
                retryCount, "Retry count must be 1 or greater.");

        var maxDelaySeconds = options.MaxDelayMinutes * 60.0;
        var rawDelay = options.BaseDelaySeconds * Math.Pow(2, retryCount - 1);

        return Math.Min(rawDelay, maxDelaySeconds);
    }
}
