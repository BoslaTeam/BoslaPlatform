using BoslaPlatform.Domain.Entities.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SpecialistDocumentConfiguration : BaseEntityConfiguration<SpecialistDocument>
    {
        public override void Configure(EntityTypeBuilder<SpecialistDocument> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Specialist)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
            builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Type).HasConversion<int>();
        }
    }
}
