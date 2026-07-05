using BoslaPlatform.Domain.Entities.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class FavoriteSpecialistConfiguration : BaseEntityConfiguration<FavoriteSpecialist>
    {
        public override void Configure(EntityTypeBuilder<FavoriteSpecialist> builder)
        {
            base.Configure(builder);
            builder.HasIndex(f => new { f.UserId, f.SpecialistId }).IsUnique();
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(f => f.Specialist)
                .WithMany()
                .HasForeignKey(f => f.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
