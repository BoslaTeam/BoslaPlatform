namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface IVideoNotifier
    {
        Task SessionStartedAsync(Guid sessionId, DateTime startedAtUtc, CancellationToken ct = default);

        Task SessionEndedAsync(Guid sessionId, DateTime endedAtUtc, CancellationToken ct = default);

        Task ParticipantJoinedAsync(Guid sessionId, Guid participantId, CancellationToken ct = default);

        Task ParticipantLeftAsync(Guid sessionId, Guid participantId, CancellationToken ct = default);

        Task RecordingStartedAsync(Guid sessionId, CancellationToken ct = default);

        Task RecordingCompletedAsync(Guid sessionId, string recordingUrl, CancellationToken ct = default);
    }
}
