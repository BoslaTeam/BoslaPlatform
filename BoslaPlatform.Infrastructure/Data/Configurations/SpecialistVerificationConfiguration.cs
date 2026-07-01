using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SpecialistVerificationConfiguration : BaseEntityConfiguration<SpecialistVerification>
    {
        public override void Configure(EntityTypeBuilder<SpecialistVerification> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Specialist)
                .WithOne(x => x.Verification)
                .HasForeignKey<SpecialistVerification>(x => x.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.AdminNotes).HasMaxLength(2000);
            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(x => x.IsSubmitted).HasDefaultValue(false);
            builder.Property(x => x.LastUpdatedAt);

            builder.HasIndex(x => x.SpecialistId).IsUnique();
        }
    }
}
