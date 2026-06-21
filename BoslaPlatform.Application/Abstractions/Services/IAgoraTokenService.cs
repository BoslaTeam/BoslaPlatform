namespace BoslaPlatform.Application.Abstractions;

/// <summary>
/// Provides Agora RTC token generation capabilities.
/// </summary>
public interface IAgoraTokenService
{
    /// <summary>
    /// Generates an RTC token for joining an Agora channel.
    /// </summary>
    /// <param name="channelName">The Agora channel name.</param>
    /// <param name="uid">The Agora user ID (uint).</param>
    /// <returns>The generated RTC token.</returns>
    string GenerateRtcToken(string channelName, uint uid);
}
