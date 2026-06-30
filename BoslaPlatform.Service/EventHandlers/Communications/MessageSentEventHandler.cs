using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
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
        private readonly INotificationService _notificationService;

        public MessageSentEventHandler(
            IAppDbContext context,
            IMapper mapper,
            IChatNotifier chatNotifier,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
            _notificationService = notificationService;
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

            if (message is null) return;

            var dto = _mapper.Map<MessageDto>(message);
            await _chatNotifier.MessageSentAsync(dto, ct);

            var conversation = await _context.Conversations
                .AsNoTracking()
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, ct);

            if (conversation is null) return;

            var recipientId = conversation.Participants
                .Select(p => p.UserId)
                .FirstOrDefault(id => id != notification.SenderId);

            if (recipientId == Guid.Empty) return;

            await _notificationService.CreateAndSendNotificationAsync(
                recipientId,
                $"رسالة جديدة من {message.Sender.Name ?? "مستخدم"}",
                notification.MessageText.Length > 100
                    ? notification.MessageText[..100] + "..."
                    : notification.MessageText,
                NotificationType.Message,
                ct);
        }
    }
}
