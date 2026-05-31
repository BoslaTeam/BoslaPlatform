using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public  void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
            builder.Property(u => u.Title).HasMaxLength(150);
            builder.Property(u => u.Bio).HasMaxLength(2000);
            builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
            builder.Property(u => u.Country).HasMaxLength(100);
            builder.Property(u => u.Gender).HasMaxLength(20);
            builder.Property(u => u.PreferredLanguage).HasMaxLength(10);

            builder.HasIndex(u => u.LastLoginAt);
            builder.HasQueryFilter(u => u.IsActive);
        }
    }
}
