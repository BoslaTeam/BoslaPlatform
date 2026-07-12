using BoslaPlatform.Domain.Entities.System;
using BoslaPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations;

public sealed class RecordingAuditLogConfiguration : IEntityTypeConfiguration<RecordingAuditLog>
{
    public void Configure(EntityTypeBuilder<RecordingAuditLog> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever(); // ID is set by the domain factory

        builder.Property(r => r.VideoSessionId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired(false);

        builder.Property(r => r.Action)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.OccurredAtUtc)
            .IsRequired();

        // FK to VideoSessions — no cascade delete; audit records are permanent
        builder.HasOne<Domain.Models.Video.VideoSession>()
            .WithMany()
            .HasForeignKey(r => r.VideoSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance: commonly queried by VideoSessionId and by OccurredAtUtc for reporting
        builder.HasIndex(r => r.VideoSessionId)
            .HasDatabaseName("IX_RecordingAuditLogs_VideoSessionId");

        builder.HasIndex(r => r.OccurredAtUtc)
            .HasDatabaseName("IX_RecordingAuditLogs_OccurredAtUtc");
    }
}
