using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistOnboardedEvent : DomainEvent
    {
        public SpecialistOnboardedEvent(Guid specialistId, Guid userId)
        {
            SpecialistId = specialistId;
            UserId = userId;
        }

        public Guid SpecialistId { get; }

        public Guid UserId { get; }
    }
}
