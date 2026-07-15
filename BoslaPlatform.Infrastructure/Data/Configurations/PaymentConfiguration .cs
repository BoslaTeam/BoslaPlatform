using BoslaPlatform.Domain.Entities.Payments;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : BaseEntityConfiguration<Payment>
    {
        public override void Configure(EntityTypeBuilder<Payment> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Amount).HasPrecision(10, 2).IsRequired();
            builder.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("EGP");
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();
            builder.Property(p => p.ExternalPaymentId).HasMaxLength(200);
            builder.Property(p => p.RefundReason).HasMaxLength(500);
            builder.Property(p => p.PlatformFeeAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            builder.Property(p => p.SpecialistAmount).HasPrecision(10, 2);
            builder.Property(p => p.TaxAmount).HasPrecision(10, 2).HasDefaultValue(0m);

            // Escrow properties
            builder.Property(p => p.EscrowStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(EscrowStatus.Held);
            builder.Property(p => p.HeldUntil);
            builder.Property(p => p.ReleasedAt);
            builder.Property(p => p.DisputeReason).HasMaxLength(500);
            builder.Property(p => p.DisputedAt);

            builder.HasOne(p => p.Appointment).WithOne(a => a.Payment)
                .HasForeignKey<Payment>(p => p.AppointmentId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.AppointmentId).IsUnique();


        }
    }
}
