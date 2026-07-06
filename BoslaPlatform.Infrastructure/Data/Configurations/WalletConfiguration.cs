using BoslaPlatform.Domain.Entities.Payouts;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.UseTpcMappingStrategy();

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.HoldBalance).HasPrecision(18, 2).IsRequired().HasDefaultValue(0);
        builder.Property(w => w.Currency).HasMaxLength(10).IsRequired().HasDefaultValue("EGP");
        builder.Property(w => w.OwnerId).IsRequired();
        builder.Navigation(w => w.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class SpecialistWalletConfiguration : IEntityTypeConfiguration<SpecialistWallet>
{
    public void Configure(EntityTypeBuilder<SpecialistWallet> builder)
    {
        builder.ToTable("SpecialistWallets");
        builder.HasOne(w => w.Specialist).WithOne().HasForeignKey<SpecialistWallet>(w => w.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.OwnerId).IsUnique();
    }
}

public class PlatformWalletConfiguration : IEntityTypeConfiguration<PlatformWallet>
{
    public void Configure(EntityTypeBuilder<PlatformWallet> builder)
    {
        builder.ToTable("PlatformWallets");
        builder.HasOne(w => w.Admin).WithMany().HasForeignKey(w => w.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.OwnerId).IsUnique();
    }
}

public class UserWalletConfiguration : IEntityTypeConfiguration<UserWallet>
{
    public void Configure(EntityTypeBuilder<UserWallet> builder)
    {
        builder.ToTable("UserWallets");
        builder.HasOne(w => w.User).WithOne().HasForeignKey<UserWallet>(w => w.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.OwnerId).IsUnique();
    }
}
