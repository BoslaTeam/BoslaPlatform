using AutoMapper;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Domain.Models.Video;

namespace BoslaPlatform.Application.Features.VideoSessions.Mapping
{
    public sealed class VideoSessionProfile : Profile
    {
        public VideoSessionProfile()
        {
            CreateMap<VideoSessionParticipant, VideoSessionParticipantDto>()
                .ForMember(
                    d => d.UserName,
                    o => o.MapFrom(s => s.User.Name))
                .ForMember(
                    d => d.Role,
                    o => o.MapFrom(s => s.Role.ToString()));

            CreateMap<ScreenRecording, RecordingInfoDto>()
                .ForMember(
                    d => d.Status,
                    o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(
                    d => d.Url,
                    o => o.MapFrom(s => s.Url))
                .ForMember(
                    d => d.CurrentRecordingId,
                    o => o.MapFrom(s => s.Id));

            CreateMap<VideoSession, VideoSessionDto>()
                .ForMember(
                    d => d.ChannelName,
                    o => o.MapFrom(s => s.AgoraChannelName))
                .ForMember(
                    d => d.Status,
                    o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(
                    d => d.Participants,
                    o => o.MapFrom(s => s.Participants))
                .ForMember(
                    d => d.Recording,
                    o => o.MapFrom(s => BuildRecordingInfo(s)))
                .ForMember(
                    d => d.AppointmentEndTime,
                    o => o.MapFrom(s => s.Appointment != null ? s.Appointment.End.UtcDateTime : (DateTime?)null));
        }

        private static RecordingInfoDto? BuildRecordingInfo(VideoSession session)
        {
            if (session.RecordingStatus is null && session.CurrentRecordingId is null)
            {
                return null;
            }

            var dto = new RecordingInfoDto
            {
                Status = session.RecordingStatus,
                StartedAtUtc = session.RecordingStartedAtUtc,
                CompletedAtUtc = session.RecordingCompletedAt,
                IsRecording = session.IsRecording,
                CurrentRecordingId = session.CurrentRecordingId,
                Url = session.RecordingUrl
            };

            if (session.CurrentRecording is not null)
            {
                dto.Url ??= session.CurrentRecording.Url;
            }

            return dto;
        }
    }
}
