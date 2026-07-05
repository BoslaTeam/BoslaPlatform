namespace BoslaPlatform.Infrastructure.AI.Tokenizers;

public class SimpleTokenizer : ITokenizer
{
    // Very naive tokenizer: 1 token ~= 4 chars
    public int CountTokens(string text) => string.IsNullOrEmpty(text) ? 0 : (int)Math.Ceiling(text.Length / 4.0);

    public string Truncate(string text, int tokenBudget)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var maxChars = tokenBudget * 4;
        return text.Length <= maxChars ? text : text.Substring(0, maxChars);
    }
}
