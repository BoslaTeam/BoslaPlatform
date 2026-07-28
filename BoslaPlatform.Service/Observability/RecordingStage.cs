namespace BoslaPlatform.Application.Observability
{
    /// <summary>
    /// The stages a recording passes through, from the first Agora call to
    /// playback. Every stage emits the same structured shape, so the stage at
    /// which a recording died is answerable from logs alone.
    /// </summary>
    public enum RecordingStage
    {
        Acquire,
        Start,
        WebhookReceived,
        Stop,
        MetadataSaved,
        PresignedUrlGenerated
    }
}
