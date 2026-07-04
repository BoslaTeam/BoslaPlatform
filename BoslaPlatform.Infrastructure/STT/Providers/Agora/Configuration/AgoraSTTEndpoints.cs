namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Configuration;

internal static class AgoraSTTEndpoints
{
    public static string BuildJoinEndpoint(string baseUrl, string appId)
    {
        return $"{baseUrl.TrimEnd('/')}/api/speech-to-text/v1/projects/{appId}/join";
    }

    public static string BuildLeaveEndpoint(string baseUrl, string appId, string agentId)
    {
        return $"{baseUrl.TrimEnd('/')}/api/speech-to-text/v1/projects/{appId}/agents/{agentId}/leave";
    }

    public static string BuildGetEndpoint(string baseUrl, string appId, string agentId)
    {
        return $"{baseUrl.TrimEnd('/')}/api/speech-to-text/v1/projects/{appId}/agents/{agentId}";
    }
}
