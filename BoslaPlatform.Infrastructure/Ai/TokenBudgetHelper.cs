namespace BoslaPlatform.Infrastructure.AI;

public static class TokenBudgetHelper
{
    // Deprecated helper kept for compatibility. Prefer ITokenizer implementations registered in DI.
    public static string TruncateToTokenBudget(string input, int tokenBudget)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var maxChars = tokenBudget * 4;
        if (input.Length <= maxChars) return input;
        return input.Substring(0, maxChars);
    }
}
