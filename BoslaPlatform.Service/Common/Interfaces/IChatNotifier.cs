using BoslaPlatform.Application.Features.Conversations.Dtos;

namespace BoslaPlatform.Application.Common.Interfaces
{
    public interface IChatNotifier
    {
        Task MessageSentAsync(
            MessageDto message,
            CancellationToken ct = default);
        Task MessageEditedAsync(
            MessageDto message,
            CancellationToken ct);
        Task MessageDeletedAsync(
            Guid conversationId,
            Guid messageId,
            CancellationToken ct = default);
    }
}
