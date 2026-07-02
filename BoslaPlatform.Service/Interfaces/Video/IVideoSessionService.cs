using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.VideoSessions.Interfaces
{
    public interface IVideoSessionService
    {
        Task<Result<VideoSessionDto>> GetByIdAsync(
            Guid sessionId,
            CancellationToken ct = default);

        Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default);

        Task<Result<StartVideoSessionResponse>> StartAsync(
            Guid videoSessionId,
            CancellationToken ct = default);

        Task<Result<EndVideoSessionResponse>> EndAsync(
            Guid videoSessionId,
            CancellationToken ct = default);

        Task<Result<StartRecordingResponse>> StartRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default);

        Task<Result<StopRecordingResponse>> StopRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default);

        Task<Result<RecordingInfoDto>> GetRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default);
    }
}
