using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistProfileUpdatedEvent(Guid specialistId) : DomainEvent
    {
        public Guid SpecialistId { get; } = specialistId;
    }
}
