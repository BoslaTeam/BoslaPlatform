using BoslaPlatform.Application.Features.VideoSessions.Dtos;

namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record StopRecordingResponse(
        Guid SessionId,
        RecordingInfoDto Recording);
}
