using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class PortfolioItemImage : BaseEntity
    {
        public Guid PortfolioItemId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public SpecialistPortfolioItem PortfolioItem { get; set; } = null!;
    }
}
