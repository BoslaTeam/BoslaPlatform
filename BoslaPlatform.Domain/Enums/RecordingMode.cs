namespace BoslaPlatform.Domain.Enums;

/// <summary>
/// Specifies the recording mode used by Agora Cloud Recording.
/// </summary>
public enum RecordingMode
{
    /// <summary>
    /// Mix mode: Mixes the audio and video of all users in a channel into a single file.
    /// </summary>
    Mix,

    /// <summary>
    /// Individual mode: Records the audio and video of each user in separate files.
    /// </summary>
    Individual,

    /// <summary>
    /// Web mode: Records the content of a web page.
    /// </summary>
    Web
}

public static class RecordingModeExtensions
{
    /// <summary>
    /// Converts the RecordingMode enum to its corresponding Agora REST API string value.
    /// </summary>
    public static string ToApiValue(this RecordingMode mode)
    {
        return mode switch
        {
            RecordingMode.Mix => "mix",
            RecordingMode.Individual => "individual",
            RecordingMode.Web => "web",
            _ => "mix" // Default to mix for safety
        };
    }
}
