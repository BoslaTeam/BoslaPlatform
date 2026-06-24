using AutoMapper;
using AutoMapper.QueryableExtensions;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Conversations.Requests;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Conversation;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Conversations.Services
{
    public sealed class MessageService : IMessageService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IMapper _mapper;

        public MessageService(
            IAppDbContext context,
            IUser currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedResult<MessageDto>>> GetAsync(
            Guid conversationId,
            PaginationRequest request,
            CancellationToken ct)
        {
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            var isParticipant = await IsParticipantAsync(conversationId, _currentUser.Id.Value, ct);

            if (!isParticipant)
            {
                return Error.Forbidden(
                    "Conversation.Forbidden",
                    "You are not a participant in this conversation.");
            }

            var query = _context.Messages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId);

            var totalCount = await query.CountAsync(ct);


            var pageNumber = request.NormalizePageNumber();
            var pageSize = request.NormalizePageSize();

            var messages = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            return Result<PaginatedResult<MessageDto>>
                .Success(
                    new PaginatedResult<MessageDto>(
                        messages,
                        PaginationMetadata.Create(
                            pageNumber,
                            pageSize,
                            totalCount)));
        }

        public async Task<Result<Guid>> SendAsync(
            Guid conversationId,
            SendMessageRequest request,
            CancellationToken ct)
        {
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(x =>
                    x.ConversationId == conversationId &&
                    x.UserId == _currentUser.Id.Value, ct);

            if (!isParticipant)
                return Error.Forbidden("Conversation.Forbidden",
                    "You are not a participant in this conversation.");


            var messageResult = Message.Create(
                conversationId,
                _currentUser.Id.Value,
                request.MessageText);

            if (messageResult.IsError)
            {
                return messageResult.Errors;
            }

            await _context.Messages.AddAsync(
                messageResult.Value,
                ct);

            await _context.SaveChangesAsync(ct);

            return messageResult.Value.Id;
        }

        public async Task<Result<bool>> EditAsync(
            Guid conversationId,
            Guid messageId,
            EditMessageRequest request,
            CancellationToken ct)
        {
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            // 1. Check Participants 
            var isParticipant = await IsParticipantAsync(conversationId,_currentUser.Id.Value, ct);
            if (!isParticipant)
            {
                return Error.Forbidden(
                    "Conversation.Forbidden",
                    "You are not a participant in this conversation.");
            }

            // 2. Get Message
            var message = await _context.Messages
                .FirstOrDefaultAsync(
                    x => x.Id == messageId &&
                         x.ConversationId == conversationId,
                    ct);

            if (message is null)
            {
                return Error.NotFound(
                    "Message.NotFound",
                    "Message was not found.");
            }
            // 3. Check Ownership
            if (message.SenderId != _currentUser.Id.Value)
            {
                return Error.Forbidden(
                    "Message.Forbidden",
                    "You can edit only your own messages.");
            }

            var result = message.Edit(
                request.MessageText);

            if (result.IsError)
            {
                return result.Errors;
            }

            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid conversationId,
            Guid messageId,
            CancellationToken ct)
        {
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            var isParticipant = await IsParticipantAsync(conversationId, _currentUser.Id.Value, ct);

            if (!isParticipant)
            {
                return Error.Forbidden(
                    "Conversation.Forbidden",
                    "You are not a participant in this conversation.");
            }
            var message = await _context.Messages
                .FirstOrDefaultAsync(
                    x => x.Id == messageId &&
                         x.ConversationId == conversationId,
                    ct);

            if (message is null)
            {
                return Error.NotFound(
                    "Message.NotFound",
                    "Message was not found.");
            }

            if (message.SenderId != _currentUser.Id.Value)
            {
                return Error.Forbidden(
                    "Message.Forbidden",
                    "You can delete only your own messages.");
            }
            // AddEvent
            message.MarkAsDeleted();
            _context.Messages.Remove(message);

            await _context.SaveChangesAsync(ct);

            return true;
        }
        private async Task<bool> IsParticipantAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct)
        {
            return await _context.ConversationParticipants
                .AnyAsync(
                    x => x.ConversationId == conversationId &&
                         x.UserId == userId,
                    ct);
        }
    }
}
