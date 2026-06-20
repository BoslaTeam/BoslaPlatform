namespace BoslaPlatform.Application.Interfaces.AI;

public interface IChatService
{
    Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default);
}
