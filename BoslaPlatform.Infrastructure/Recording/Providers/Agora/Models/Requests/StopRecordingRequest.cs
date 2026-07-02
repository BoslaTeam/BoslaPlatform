namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests
{
    internal sealed record StopRecordingRequest
    {
        public string Cname { get; init; } = string.Empty;

        public string Uid { get; init; } = string.Empty;

        public StopClientRequest ClientRequest { get; init; } = new();
    }

    internal sealed record StopClientRequest
    {
    }
}
