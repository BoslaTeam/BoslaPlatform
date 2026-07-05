namespace BoslaPlatform.Application.Features.Conversations.Dtos
{
    public sealed record MessageDeletedDto(
    Guid ConversationId,
    Guid MessageId);
}
