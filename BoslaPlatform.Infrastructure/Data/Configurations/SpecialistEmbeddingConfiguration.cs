using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SpecialistEmbeddingConfiguration : BaseEntityConfiguration<SpecialistEmbedding>
    {
        public override void Configure(EntityTypeBuilder<SpecialistEmbedding> builder)
        {
            base.Configure(builder);
            builder.Property(se => se.EmbeddingModel).HasMaxLength(50).IsRequired();
            builder.Property(se => se.ContentHash).HasMaxLength(64).IsRequired();

            builder.HasOne(se => se.Specialist)
                .WithOne(s => s.Embedding)
                .HasForeignKey<SpecialistEmbedding>(se => se.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(se => se.SpecialistId).IsUnique();
        }
    }
}
