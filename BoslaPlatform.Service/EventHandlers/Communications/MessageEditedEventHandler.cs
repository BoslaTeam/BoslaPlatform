using AutoMapper;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Conversations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.EventHandlers.Communications
{
    public sealed class MessageEditedEventHandler
    : INotificationHandler<MessageEditedEvent>
    {
        private readonly IChatNotifier _chatNotifier;
        private readonly ILogger<MessageEditedEventHandler> _logger;
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public MessageEditedEventHandler(
            IChatNotifier chatNotifier,
            ILogger<MessageEditedEventHandler> logger,
            IAppDbContext context,
            IMapper mapper)
        {
            _chatNotifier = chatNotifier;
            _logger = logger;
           _context = context;
            _mapper = mapper;
        }

        public async Task Handle(
            MessageEditedEvent notification,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "[REALTIME DEBUG] MessageEditedEventHandler.Handle - MessageId: {MessageId}, ConversationId: {ConversationId}",
                notification.MessageId,
                notification.ConversationId);

            try
            {
                var message = await _context.Messages
                    .AsNoTracking()
                    .Include(x => x.Sender)
                    .FirstAsync(x => x.Id == notification.MessageId, ct);

                var dto = _mapper.Map<MessageDto>(message);

                await _chatNotifier.MessageEditedAsync(dto, ct);

                _logger.LogInformation(
                    "[REALTIME DEBUG] MessageEditedAsync sent successfully for MessageId: {MessageId}",
                    notification.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[REALTIME DEBUG] Error in MessageEditedAsync for MessageId: {MessageId}",
                    notification.MessageId);
                throw;
            }
        }
    }
}
