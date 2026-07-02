using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class SpecialistProfileEmbeddingNeededEvent : DomainEvent
    {
        public Guid SpecialistId { get; }
        public bool ForceRebuild { get; }

        public SpecialistProfileEmbeddingNeededEvent(Guid specialistId, bool forceRebuild = false)
        {
            SpecialistId = specialistId;
            ForceRebuild = forceRebuild;
        }
    }
}
