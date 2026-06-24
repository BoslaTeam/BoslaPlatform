using AutoMapper;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Domain.Models.Communication;

namespace BoslaPlatform.Application.Features.Conversations.Mappings
{
    public sealed class ConversationMappingProfile : Profile
    {
        public ConversationMappingProfile()
        {
            CreateMap<ConversationParticipant, ConversationParticipantDto>()
                .ForMember(
                    d => d.FullName,
                    o => o.MapFrom(s => s.User.Name))
                .ForMember(
                    d => d.ProfilePictureUrl,
                    o => o.MapFrom(s => s.User.ProfileImageUrl));

            CreateMap<Message, MessageDto>()
                .ForMember(
                    d => d.SenderName,
                    o => o.MapFrom(s => s.Sender.Name));

            CreateMap<Conversation, ConversationDto>()
                .ForMember(d => d.LastMessage, o => o.MapFrom(
                    s => s.Messages.FirstOrDefault()));
        }
    }
}
