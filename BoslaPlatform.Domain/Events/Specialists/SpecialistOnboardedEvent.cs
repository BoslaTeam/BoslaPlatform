using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistOnboardedEvent(Guid userId) : DomainEvent
    {
        public Guid UserId { get; } = userId;
    }
}
