using AutoMapper;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Conversations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Communications
{
    public sealed class MessageSentEventHandler
        : INotificationHandler<MessageSentEvent>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IChatNotifier _chatNotifier;

        public MessageSentEventHandler(
            IAppDbContext context,
            IMapper mapper,
            IChatNotifier chatNotifier)
        {
            _context = context;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
        }

        public async Task Handle(
            MessageSentEvent notification,
            CancellationToken ct)
        {
            var message = await _context.Messages
                .AsNoTracking()
                .Include(x => x.Sender)
                .FirstOrDefaultAsync(
                    x => x.Id == notification.MessageId,
                    ct);

            if (message is null)
            {
                return;
            }

            var dto =
                _mapper.Map<MessageDto>(message);

            await  _chatNotifier.MessageSentAsync(dto, ct);
        }
    }
}
