using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class PortfolioItemSubmittedEvent(Guid specialistId, Guid portfolioItemId, string title) : DomainEvent
    {
        public Guid SpecialistId { get; } = specialistId;
        public Guid PortfolioItemId { get; } = portfolioItemId;
        public string Title { get; } = title;
    }
}
