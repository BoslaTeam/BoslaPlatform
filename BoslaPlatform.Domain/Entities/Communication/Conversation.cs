using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Conversations;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Domain.Models.Communication
{
    public class Conversation : AuditableEntity
    {
        private readonly List<ConversationParticipant> _participants = [];
        private readonly List<Message> _messages = [];

        private Conversation() { }

        public Guid AppointmentId { get; private set; }
        public Appointment Appointment { get; private set; } = null!;
        public IReadOnlyCollection<ConversationParticipant> Participants
            => _participants.AsReadOnly();
        public IReadOnlyCollection<Message> Messages
            => _messages.AsReadOnly();
        public static Result<Conversation> CreateForAppointment(
            Guid appointmentId,
            Guid userId,
            Guid specialistId)
        {
            if (userId == specialistId)
            {
                return Error.Validation(
                    "Conversation.InvalidParticipants",
                    "Participants must be different.");
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId
            };

            conversation._participants.Add(
                ConversationParticipant.Create(conversation.Id,userId));

            conversation._participants.Add(
                ConversationParticipant.Create(conversation.Id,specialistId));

            conversation.AddDomainEvent(
                new ConversationCreatedEvent(
                    conversation.Id,
                    appointmentId));

            return conversation;
        }

        //public Result AddParticipant(Guid userId, ParticipantRole role)
        //{
        //    if (_participants.Any(x => x.UserId == userId))
        //    {
        //        return Result.Failure(Error.Conflict(
        //            "Conversation.ParticipantExists",
        //            "Participant already exists."));
        //    }

        //    _participants.Add(
        //        ConversationParticipant.Create(
        //            Id,
        //            userId));

        //    return Result.Success();
        //}

        //public Result RemoveParticipant(Guid userId)
        //{
        //    var participant = _participants
        //        .FirstOrDefault(x => x.UserId == userId);

        //    if (participant is null)
        //    {
        //        return Result.Failure( Error.NotFound(
        //            "ConversationParticipant.NotFound",
        //            "Participant was not found."));
        //    }

        //    _participants.Remove(participant);

        //    return Result.Success();
        //}

}
}
