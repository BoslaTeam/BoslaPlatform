using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// A dedicated, single-table context for recording pipeline telemetry.
///
/// WHY SEPARATE FROM AppDbContext:
///   Pipeline events must survive the rollback of the domain transaction that
///   produced them — a stage that failed inside a rolled-back unit of work must
///   still leave a record. A distinct context on its own connection guarantees
///   that isolation, and keeps telemetry writes off the domain change tracker.
/// </summary>
public sealed class RecordingDiagnosticsDbContext : DbContext
{
    public RecordingDiagnosticsDbContext(DbContextOptions<RecordingDiagnosticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecordingPipelineEvent> RecordingPipelineEvents => Set<RecordingPipelineEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<RecordingPipelineEvent>();
        e.ToTable("RecordingPipelineEvents");
        e.HasKey(x => x.Id);

        e.Property(x => x.RecordingCorrelationId).HasMaxLength(128);
        e.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        e.Property(x => x.Stage).HasMaxLength(48).IsRequired();
        e.Property(x => x.Outcome).HasMaxLength(16).IsRequired();
        // Agora resourceIds are long (~430+ chars); 256 truncates every write.
        e.Property(x => x.ResourceId).HasMaxLength(1024);
        e.Property(x => x.Sid).HasMaxLength(256);
        e.Property(x => x.ChannelName).HasMaxLength(256);
        e.Property(x => x.ErrorCode).HasMaxLength(128);
        e.Property(x => x.ErrorDescription).HasMaxLength(2048);
        e.Property(x => x.Detail).HasMaxLength(4000);

        // The two access paths: stitch by correlation id, stitch by provider sid.
        e.HasIndex(x => x.RecordingCorrelationId);
        e.HasIndex(x => x.Sid);
        e.HasIndex(x => x.OccurredAtUtc);
    }

    /// <summary>
    /// Idempotent table bootstrap. The main schema is owned by AppDbContext's
    /// migration chain; rather than entangle this telemetry table in it, the
    /// table is created on first run if absent. Safe to run on every startup.
    /// </summary>
    public async Task EnsureTableExistsAsync(CancellationToken ct = default)
    {
        const string ddl = """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecordingPipelineEvents')
            BEGIN
                CREATE TABLE [RecordingPipelineEvents] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [RecordingCorrelationId] NVARCHAR(128) NULL,
                    [SessionId] UNIQUEIDENTIFIER NULL,
                    [AppointmentId] UNIQUEIDENTIFIER NULL,
                    [RecordingId] UNIQUEIDENTIFIER NULL,
                    [Provider] NVARCHAR(32) NOT NULL,
                    [Stage] NVARCHAR(48) NOT NULL,
                    [Outcome] NVARCHAR(16) NOT NULL,
                    [Attempt] INT NOT NULL DEFAULT(1),
                    [ResourceId] NVARCHAR(1024) NULL,
                    [Sid] NVARCHAR(256) NULL,
                    [ChannelName] NVARCHAR(256) NULL,
                    [DurationMs] FLOAT NULL,
                    [ErrorCode] NVARCHAR(128) NULL,
                    [ErrorDescription] NVARCHAR(2048) NULL,
                    [Detail] NVARCHAR(4000) NULL,
                    [OccurredAtUtc] DATETIME2 NOT NULL
                );
                CREATE INDEX [IX_RecordingPipelineEvents_CorrelationId] ON [RecordingPipelineEvents]([RecordingCorrelationId]);
                CREATE INDEX [IX_RecordingPipelineEvents_Sid] ON [RecordingPipelineEvents]([Sid]);
                CREATE INDEX [IX_RecordingPipelineEvents_OccurredAtUtc] ON [RecordingPipelineEvents]([OccurredAtUtc]);
            END

            -- Widen ResourceId on databases created before the column was enlarged.
            -- (NVARCHAR(256) => sys.columns.max_length 512; widen anything under 2048.)
            IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('RecordingPipelineEvents')
                  AND name = 'ResourceId' AND max_length < 2048)
            BEGIN
                ALTER TABLE [RecordingPipelineEvents] ALTER COLUMN [ResourceId] NVARCHAR(1024) NULL;
            END
            """;

        await Database.ExecuteSqlRawAsync(ddl, ct);
    }
}
