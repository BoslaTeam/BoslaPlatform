namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public static class AgoraEventType
    {
        public const int ChannelCreate = 101; // V1 channel created
        public const int ChannelDestroy = 102; // V1 channel destroyed
        public const int BroadcasterJoin = 103; // participant joined
        public const int BroadcasterLeave = 104; // participant left
        public const int ChannelCreateV2 = 110; // V2 channel created
        public const int ChannelDestroyV2 = 111; // V2 channel destroyed
        public const int RecordingStarted = 1001; // cloud recording started
        public const int RecordingStopped = 1003; // cloud recording stopped
        public const int RecordingUploaded = 1004; // cloud recording uploaded to storage provider
    }
}

