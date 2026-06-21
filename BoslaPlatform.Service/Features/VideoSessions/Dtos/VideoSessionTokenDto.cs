namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    public sealed class VideoSessionTokenDto
    {
        public string AppId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public uint AgoraUid { get; set; }  // uint مش long
    }
}
