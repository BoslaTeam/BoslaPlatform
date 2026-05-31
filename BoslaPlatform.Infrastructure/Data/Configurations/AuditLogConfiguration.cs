using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
    {
        public override void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(a => a.IpAddress).HasMaxLength(50);


            builder.HasIndex(a => new { a.EntityType, a.EntityId });
            builder.HasOne(a => a.ChangedByUser).WithMany().HasForeignKey(a => a.ChangedByUser)
          .OnDelete(DeleteBehavior.NoAction);
        }
    }

}
