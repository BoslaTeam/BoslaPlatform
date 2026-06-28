using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Domain.Events.Conversations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Communications
{
    public sealed class MessageDeletedEventHandler
    : INotificationHandler<MessageDeletedEvent>
    {
        private readonly IChatNotifier _chatNotifier;
        private readonly ILogger<MessageDeletedEventHandler> _logger;

        public MessageDeletedEventHandler(
            IChatNotifier chatNotifier,
            ILogger<MessageDeletedEventHandler> logger)
        {
            _chatNotifier = chatNotifier;
            _logger = logger;
        }

        public async Task Handle(
            MessageDeletedEvent notification,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "[REALTIME DEBUG] MessageDeletedEventHandler.Handle - MessageId: {MessageId}, ConversationId: {ConversationId}",
                notification.MessageId,
                notification.ConversationId);

            try
            {
                await _chatNotifier.MessageDeletedAsync(
                    notification.ConversationId,
                    notification.MessageId,
                    ct);

                _logger.LogInformation(
                    "[REALTIME DEBUG] MessageDeletedAsync sent successfully for MessageId: {MessageId}",
                    notification.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[REALTIME DEBUG] Error in MessageDeletedAsync for MessageId: {MessageId}",
                    notification.MessageId);
                throw;
            }
        }
    }
}
