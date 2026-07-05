using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SpecialistPortfolioItemConfiguration : BaseEntityConfiguration<SpecialistPortfolioItem>
    {
        public override void Configure(EntityTypeBuilder<SpecialistPortfolioItem> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.CoverImageUrl).HasMaxLength(1000).IsRequired();
            builder.Property(p => p.WorkUrl).HasMaxLength(2000);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired().HasDefaultValue(PortfolioItemStatus.Pending);
            builder.Property(p => p.AdminNotes).HasMaxLength(500);
            builder.Property(p => p.SortOrder).HasDefaultValue(0);
            builder.HasOne(p => p.Specialist)
                .WithMany(s => s.PortfolioItems)
                .HasForeignKey(p => p.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(p => p.Images)
                .WithOne(i => i.PortfolioItem)
                .HasForeignKey(i => i.PortfolioItemId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(p => new { p.SpecialistId, p.SortOrder });
            builder.HasIndex(p => new { p.SpecialistId, p.Status });
        }
    }
}
