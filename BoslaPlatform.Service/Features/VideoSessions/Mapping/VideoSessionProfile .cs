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
                    o => o.MapFrom(s => s.User.Name));

            CreateMap<VideoSession, VideoSessionDto>()
                .ForMember(
                    d => d.ChannelName,
                    o => o.MapFrom(s => s.AgoraChannelName))
                .ForMember(
                    d => d.Participants,
                    o => o.MapFrom(s => s.Participants));
        }
    }
}
