namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public static class AgoraEventType
    {
        public const int ChannelCreate = 101; // session started
        public const int ChannelDestroy = 102; // session ended
        public const int BroadcasterJoin = 103; // participant joined
        public const int BroadcasterLeave = 104; // participant left
    }
}
