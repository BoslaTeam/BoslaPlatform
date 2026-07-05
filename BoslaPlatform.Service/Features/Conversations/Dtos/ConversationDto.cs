namespace BoslaPlatform.Application.Features.Conversations.Dtos
{
    public sealed class ConversationDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public List<ConversationParticipantDto> Participants { get; set; } = [];
        public MessageDto? LastMessage { get; set; }
    }
}
