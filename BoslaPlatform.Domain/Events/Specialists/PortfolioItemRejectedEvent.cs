using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Specialists
{
    public sealed class PortfolioItemRejectedEvent(Guid specialistId, Guid portfolioItemId, string title, string? reason) : DomainEvent
    {
        public Guid SpecialistId { get; } = specialistId;
        public Guid PortfolioItemId { get; } = portfolioItemId;
        public string Title { get; } = title;
        public string? Reason { get; } = reason;
    }
}
