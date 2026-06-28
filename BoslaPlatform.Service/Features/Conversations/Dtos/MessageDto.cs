namespace BoslaPlatform.Application.Features.Conversations.Dtos
{
    public sealed class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public bool IsEdited { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? LastModifiedUtc { get; set; }
    }
}
