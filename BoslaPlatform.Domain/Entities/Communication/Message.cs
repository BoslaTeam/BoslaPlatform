using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Events.Conversations;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Domain.Models.Communication
{
    public class Message : AuditableEntity
    {
        private Message() { }

        public Guid ConversationId { get; private set; }
        public Guid SenderId { get; private set; }
        public string MessageText { get; private set; } = string.Empty;
        public bool IsEdited { get; private set; }
        public Conversation Conversation { get; private set; } = null!;

        public User Sender { get; private set; } = null!;

        public static Result<Message> Create(
            Guid conversationId,
            Guid senderId,
            string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return Error.Validation(
                    "Message.Empty",
                    "Message cannot be empty.");
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                MessageText = messageText.Trim()
            };

            message.AddDomainEvent(
                new MessageSentEvent(
                    message.Id,
                    conversationId,
                    senderId,
                    messageText));

            return message;
        }

        public Result Edit(string newText)
        {
            if (string.IsNullOrWhiteSpace(newText))
            {
                return Result.Failure(
                    Error.Validation(
                        "Message.Empty",
                        "Message cannot be empty."));
            }

            MessageText = newText.Trim();
            IsEdited = true;

            AddDomainEvent(
                new MessageEditedEvent(
                    Id,
                    ConversationId));

            return Result.Success();
        }
        public void MarkAsDeleted()
        {
            AddDomainEvent(
                new MessageDeletedEvent(
                    Id,
                    ConversationId));
        }
    }
}
