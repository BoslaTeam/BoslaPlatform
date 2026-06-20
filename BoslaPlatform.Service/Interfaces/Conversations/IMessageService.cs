using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Conversations.Requests;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;

namespace BoslaPlatform.Application.Interfaces.Conversation
{
    public interface IMessageService
    {
        Task<Result<PaginatedResult<MessageDto>>> GetAsync(Guid conversationId,PaginationRequest request,CancellationToken ct);
        Task<Result<Guid>> SendAsync(Guid conversationId,SendMessageRequest request,CancellationToken ct);
        Task<Result<bool>> EditAsync(Guid conversationId,Guid messageId,EditMessageRequest request,CancellationToken ct);
        Task<Result<bool>> DeleteAsync(Guid conversationId,Guid messageId,CancellationToken ct);
        //CreateMessage -> SaveChanges -> MessageSentEvent -> Handler -> SignalR -> Notification -> Push Notification
    }
}
