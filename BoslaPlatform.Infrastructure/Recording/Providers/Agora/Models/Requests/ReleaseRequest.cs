namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests
{
    internal sealed record ReleaseRequest
    {
        public string Cname { get; init; } = string.Empty;

        public string Uid { get; init; } = string.Empty;

        public ReleaseClientRequest ClientRequest { get; init; } = new();
    }

    internal sealed record ReleaseClientRequest
    {
    }
}
