using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistVerificationSubmittedEvent(Guid specialistId) : DomainEvent
    {
        public Guid SpecialistId { get; } = specialistId;
    }
}
