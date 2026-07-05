namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Responses
{
    internal sealed record AcquireResponse
    {
        public string ResourceId { get; init; } = string.Empty;
    }
}
