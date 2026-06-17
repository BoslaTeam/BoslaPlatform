using AutoMapper;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Conversations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.EventHandlers.Communications
{
    public sealed class MessageEditedEventHandler
    : INotificationHandler<MessageEditedEvent>
    {
        private readonly IChatNotifier _chatNotifier;
        public MessageEditedEventHandler(
            IChatNotifier chatNotifier)
        {
            _chatNotifier = chatNotifier;
        }

        public async Task Handle(
            MessageEditedEvent notification,
            CancellationToken ct)
        {
            await _chatNotifier.MessageEditedAsync(
                notification.ConversationId,
                notification.MessageId,
                ct);
        }
    }
}
