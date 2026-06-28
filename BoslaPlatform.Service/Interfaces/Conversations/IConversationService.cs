using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Conversations.Requests;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;

namespace BoslaPlatform.Application.Interfaces.Conversation
{
    public interface IConversationService
    {
        Task<Result<Guid>> CreateAsync(CreateConversationRequest request,CancellationToken ct);
        Task<Result<ConversationDto>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<PaginatedResult<ConversationDto>>> GetMyConversationsAsync(PaginationRequest request,CancellationToken ct);

        //Task<Result<bool>> AddParticipantAsync(Guid conversationId,Guid userId,CancellationToken ct);

        //Task<Result<bool>> RemoveParticipantAsync(Guid conversationId, Guid userId,CancellationToken ct);
    }
}
