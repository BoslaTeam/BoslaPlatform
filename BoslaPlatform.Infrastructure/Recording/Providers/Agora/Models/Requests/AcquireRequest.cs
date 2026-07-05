namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests
{
    internal sealed record AcquireRequest
    {
        public string Cname { get; init; } = string.Empty;

        public string Uid { get; init; } = string.Empty;

        public AcquireClientRequest ClientRequest { get; init; } = new();
    }

    internal sealed record AcquireClientRequest
    {
    }
}
