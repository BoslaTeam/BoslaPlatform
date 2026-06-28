using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Shared.Constants;
using Microsoft.AspNetCore.SignalR;

namespace BoslaPlatform.Infrastructure.Realtime
{
    public sealed class SignalRChatNotifier : IChatNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRChatNotifier(
            IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task MessageSentAsync(
            MessageDto message,
            CancellationToken ct = default)
        {
            await _hubContext.Clients
                .Group(message.ConversationId.ToString())
                .SendAsync(
                    SignalREvents.MessageReceived,
                    message,
                    ct);
        }

        public async Task MessageEditedAsync(MessageDto message, CancellationToken ct)
        {
            await _hubContext.Clients
                .Group(message.ConversationId.ToString())
                .SendAsync(
                    SignalREvents.MessageEdited,
                    message,
                    ct);
        }

        public async Task MessageDeletedAsync(Guid conversationId,Guid messageId,CancellationToken ct = default)
        {
            await _hubContext.Clients
                .Group(conversationId.ToString())
                .SendAsync(
                    SignalREvents.MessageDeleted,
                    new MessageDeletedDto(
                        conversationId,
                        messageId),
                    ct);
        }
    }
}
