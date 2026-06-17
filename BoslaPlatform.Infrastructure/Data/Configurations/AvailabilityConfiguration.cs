using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class AvailabilityConfiguration : BaseEntityConfiguration<Availability>
    {
        public override void Configure(EntityTypeBuilder<Availability> builder)
        {
            base.Configure(builder);
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.Specialist).WithMany(s => s.Availabilities)
                .HasForeignKey(a => a.SpecialistId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new
            {
                a.SpecialistId,
                a.Start,
                a.End
            });
        }
    }
}
