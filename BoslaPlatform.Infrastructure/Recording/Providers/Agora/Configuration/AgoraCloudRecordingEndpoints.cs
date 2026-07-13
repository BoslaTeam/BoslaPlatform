namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Configuration
{
    internal static class AgoraCloudRecordingEndpoints
    {
        private const string V1Path = "/v1/apps";
        private const string AcquireSegment = "acquire";
        private const string StartSegment = "start";
        private const string StopSegment = "stop";
        private const string QuerySegment = "query";
        private const string ReleaseSegment = "release";

        public static string BuildAcquireEndpoint(string baseUrl, string appId)
        {
            return $"{baseUrl.TrimEnd('/')}{V1Path}/{appId}/cloud_recording/{AcquireSegment}";
        }

        public static string BuildStartEndpoint(string baseUrl, string appId, string resourceId, string mode)
        {
            return $"{baseUrl.TrimEnd('/')}{V1Path}/{appId}/cloud_recording/resourceid/{resourceId}/mode/{mode}/{StartSegment}";
        }

        public static string BuildStopEndpoint(string baseUrl, string appId, string resourceId, string sid, string mode)
        {
            return $"{baseUrl.TrimEnd('/')}{V1Path}/{appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/{mode}/{StopSegment}";
        }

        public static string BuildQueryEndpoint(string baseUrl, string appId, string resourceId, string sid, string mode)
        {
            return $"{baseUrl.TrimEnd('/')}{V1Path}/{appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/{mode}/{QuerySegment}";
        }

        public static string BuildReleaseEndpoint(string baseUrl, string appId, string resourceId)
        {
            return $"{baseUrl.TrimEnd('/')}{V1Path}/{appId}/cloud_recording/resourceid/{resourceId}/{ReleaseSegment}";
        }
    }
}
