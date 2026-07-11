using BoslaPlatform.Infrastructure.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="OutboxMessage"/> entity.
/// Configures table mapping, keys, property constraints, and indexes for efficient outbox processing.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EventType)
            .HasMaxLength(OutboxConstants.EventTypeMaxLength)
            .IsRequired();

        builder.Property(x => x.AssemblyName)
            .HasMaxLength(OutboxConstants.AssemblyNameMaxLength)
            .IsRequired();

        // EventVersion has no SQL default — the C# property initialiser on
        // OutboxMessage (→ OutboxConstants.EventVersion) is the single source of truth.
        builder.Property(x => x.EventVersion)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.CorrelationId);

        builder.Property(x => x.OccurredOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessingStartedUtc);

        builder.Property(x => x.LastError)
            .HasMaxLength(OutboxConstants.ErrorMaxLength);

        // Index: fetch unprocessed messages efficiently
        builder.HasIndex(x => x.ProcessedOnUtc)
            .HasDatabaseName("IX_OutboxMessages_ProcessedOnUtc");

        // Index: sort unprocessed messages by occurrence order
        builder.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc })
            .HasDatabaseName("IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc");

        // Index: query messages by creation timestamp (e.g., for cleanup or monitoring)
        builder.HasIndex(x => x.OccurredOnUtc)
            .HasDatabaseName("IX_OutboxMessages_OccurredOnUtc");
    }
}
