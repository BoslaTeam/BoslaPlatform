using AutoMapper;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Domain.Models.Video;

namespace BoslaPlatform.Application.Features.VideoSessions.Mapping
{
    /// <summary>
    /// AutoMapper profile for mapping video session domain entities to DTOs.
    /// </summary>
    public sealed class VideoSessionProfile : Profile
    {
        /// <summary>
        /// Initializes mapping configurations for VideoSession and VideoSessionParticipant.
        /// </summary>
        public VideoSessionProfile()
        {
            CreateMap<VideoSessionParticipant, VideoSessionParticipantDto>()
                .ForMember(
                    d => d.UserName,
                    o => o.MapFrom(s => s.User.Name))
                .ForMember(
                    d => d.Role,
                    o => o.MapFrom(s => s.Role.ToString()));

            CreateMap<VideoSession, VideoSessionDto>()
                .ForMember(
                    d => d.ChannelName,
                    o => o.MapFrom(s => s.AgoraChannelName))
                .ForMember(
                    d => d.Status,
                    o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(
                    d => d.Participants,
                    o => o.MapFrom(s => s.Participants));
        }
    }
}
