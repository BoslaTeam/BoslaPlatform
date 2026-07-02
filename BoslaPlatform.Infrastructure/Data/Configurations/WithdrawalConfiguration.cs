using BoslaPlatform.Domain.Entities.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations;

public class WithdrawalConfiguration : BaseEntityConfiguration<Withdrawal>
{
    public override void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PaymentDetails).HasMaxLength(500);
        builder.Property(x => x.AdminNotes).HasMaxLength(2000);

        builder.HasOne(x => x.Specialist)
            .WithMany()
            .HasForeignKey(x => x.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SpecialistId);
        builder.HasIndex(x => x.Status);
    }
}
