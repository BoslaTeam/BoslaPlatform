using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class SpecialistPortfolioItem : AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public string? WorkUrl { get; set; }
        public PortfolioItemStatus Status { get; set; } = PortfolioItemStatus.Pending;
        public string? AdminNotes { get; set; }
        public int SortOrder { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public ICollection<PortfolioItemImage> Images { get; set; } = new List<PortfolioItemImage>();
    }
}
