using BoslaPlatform.Domain.Entities.Payments;
using BoslaPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class PaymentComplaintConfiguration : BaseEntityConfiguration<PaymentComplaint>
    {
        public override void Configure(EntityTypeBuilder<PaymentComplaint> builder)
        {
            base.Configure(builder);
            builder.Property(c => c.Reason).HasMaxLength(200).IsRequired();
            builder.Property(c => c.Description).HasMaxLength(2000);
            builder.Property(c => c.Status)
                 .HasConversion<string>()
                 .HasMaxLength(20)
                 .IsRequired()
                 .HasDefaultValue(ComplaintStatus.Pending);
            builder.Property(c => c.AdminNotes).HasMaxLength(1000);

            builder.HasOne(c => c.Payment)
                .WithOne(p => p.Complaint)
                .HasForeignKey<PaymentComplaint>(c => c.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.PaymentId).IsUnique();
            builder.HasIndex(c => c.Status);
        }
    }
}
