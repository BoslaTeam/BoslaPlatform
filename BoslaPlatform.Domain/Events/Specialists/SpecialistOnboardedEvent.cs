using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistOnboardedEvent(Guid specialistId, Guid userId) : DomainEvent
    {
        public Guid SpecialistId { get; } = specialistId;

        public Guid UserId { get; } = userId;
    }
}
