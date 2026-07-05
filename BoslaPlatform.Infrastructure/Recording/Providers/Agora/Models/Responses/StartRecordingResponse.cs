namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Responses
{
    internal sealed record StartRecordingResponse
    {
        public string ResourceId { get; init; } = string.Empty;

        public string Sid { get; init; } = string.Empty;
    }
}
