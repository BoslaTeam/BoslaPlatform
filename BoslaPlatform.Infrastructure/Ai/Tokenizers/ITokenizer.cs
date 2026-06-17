namespace BoslaPlatform.Infrastructure.AI.Tokenizers;

public interface ITokenizer
{
    int CountTokens(string text);
    string Truncate(string text, int tokenBudget);
}
