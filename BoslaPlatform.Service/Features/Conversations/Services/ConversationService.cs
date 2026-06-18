using AutoMapper;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Conversations.Requests;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Conversation;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Conversations.Services
{
    public sealed class ConversationService : IConversationService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IMapper _mapper;

        public ConversationService(
            IAppDbContext context,
            IUser currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }
        // TODO:
        // In future versions, conversations should be created automatically
        // when an appointment becomes confirmed through AppointmentConfirmedEvent.
        public async Task<Result<Guid>> CreateAsync(
        CreateConversationRequest request,
            CancellationToken ct)
        {

            // 1. Check if a appointment exists between the users
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(x => x.Id == request.AppointmentId, ct);

            if (appointment is null)
            {
                return Error.NotFound(
                    "Appointment.NotFound",
                    "Appointment was not found.");
            }
            // 2. Check if the current user is either the specialist or the client in the appointment
            if (appointment.UserId != _currentUser.Id && appointment.SpecialistId != _currentUser.Id)
            {
                return Error.Forbidden(
                    "Conversation.Forbidden",
                    "You are not part of this appointment.");
            }
            // 3. Checl if Appointment Status is already confirmed
            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                return Error.Validation(
                    "Appointment.NotConfirmed",
                    "Conversation can only be created for confirmed appointments.");
            }

            // 4. Check if conversation already exists for the appointment
            var exists = await _context.Conversations
                .AnyAsync(x => x.AppointmentId == appointment.Id, ct);

            if (exists)
            {
                return Error.Conflict(
                    "Conversation.Exists",
                    "Conversation already exists.");
            }
            // 3. Create conversation
            var conversationResult =
                Conversation.CreateForAppointment(
                    appointment.Id,
                    appointment.UserId,
                    appointment.SpecialistId);

            if (conversationResult.IsError)
            {
                return Result<Guid>.Failure(
                    conversationResult.Errors);
            }
            // 4. Save conversation
            await _context.Conversations.AddAsync(conversationResult.Value,ct);

            await _context.SaveChangesAsync(ct);

            return conversationResult.Value.Id;
        }
        public async Task<Result<ConversationDto>> GetByIdAsync(
            Guid id,
            CancellationToken ct)
        {
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            // 1. Retrieve conversation with related data
            var conversation = await _context.Conversations
                    .AsNoTracking()
                    .Include(x => x.Participants)
                    .ThenInclude(x => x.User)
                    .FirstOrDefaultAsync(
                    x => x.Id == id &&
                    x.Participants.Any(
                 p => p.UserId == _currentUser.Id!.Value), ct);

            if (conversation is null)
            {
                return Error.NotFound(
                    "Conversation.NotFound",
                    "Conversation was not found.");
            }

            return _mapper.Map<ConversationDto>(conversation);
        }

        public async Task<Result<PaginatedResult<ConversationDto>>>
            GetMyConversationsAsync(
                PaginationRequest request,
                CancellationToken ct)
        {
            // 1. Validate user authentication
            if (_currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            // 2. Retrieve conversations with pagination
            var query = _context.Conversations
                .AsNoTracking()
                .Where(x =>
                    x.Participants.Any(
                        p => p.UserId == _currentUser.Id.Value));

            // 3. Get total count for pagination metadata
            var totalCount = await query.CountAsync(ct);

            var pageNumber = request.NormalizePageNumber();
            var pageSize = request.NormalizePageSize();

            // 4. Include related data and apply pagination
            var conversations = await query
                .Include(x => x.Participants)
                 .ThenInclude(x => x.User)
                .Include(x => x.Messages
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(1))
                .ThenInclude(x => x.Sender)
                .OrderByDescending(x => x.LastModifiedUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // 5. Map to DTOs and return paginated result
            return new PaginatedResult<ConversationDto>(
                _mapper.Map<List<ConversationDto>>(conversations),
                PaginationMetadata.Create(
                    pageNumber,
                    pageSize,
                    totalCount));
        }
    }
}
