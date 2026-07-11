using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Validates <see cref="RateLimitingSettings"/> at application startup to reject misconfigured policies
/// before the rate limiter middleware becomes active.
/// </summary>
public sealed class RateLimitingSettingsValidation : IValidateOptions<RateLimitingSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RateLimitingSettings options)
    {
        var errors = new List<string>();

        foreach (var (policyName, policyOptions) in options.Policies)
        {
            if (policyOptions.PermitLimit <= 0)
                errors.Add(
                    $"Rate limiting policy '{policyName}': " +
                    $"PermitLimit must be greater than 0 (current: {policyOptions.PermitLimit}).");

            if (policyOptions.WindowSeconds <= 0)
                errors.Add(
                    $"Rate limiting policy '{policyName}': " +
                    $"WindowSeconds must be greater than 0 (current: {policyOptions.WindowSeconds}).");

            if (policyOptions.QueueLimit < 0)
                errors.Add(
                    $"Rate limiting policy '{policyName}': " +
                    $"QueueLimit cannot be negative (current: {policyOptions.QueueLimit}).");
        }

        return errors.Count switch
        {
            0 => ValidateOptionsResult.Success,
            1 => ValidateOptionsResult.Fail(errors[0]),
            _ => ValidateOptionsResult.Fail(errors)
        };
    }
}
