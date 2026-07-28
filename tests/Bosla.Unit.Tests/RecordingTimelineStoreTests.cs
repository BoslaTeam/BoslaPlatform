using BoslaPlatform.Application.Observability;
using BoslaPlatform.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bosla.Unit.Tests
{
    /// <summary>
    /// Proves the timeline is reconstructed from persisted events alone — the
    /// diagnostics endpoint's core promise — and that provider stages which only
    /// knew the SID are stitched onto the canonical correlation id.
    /// </summary>
    public class RecordingTimelineStoreTests
    {
        private static RecordingDiagnosticsDbContext NewContext() =>
            new(new DbContextOptionsBuilder<RecordingDiagnosticsDbContext>()
                .UseInMemoryDatabase($"Timeline_{Guid.NewGuid()}")
                .Options);

        private static RecordingPipelineEvent Evt(
            string stage, string outcome, DateTime at,
            string? correlationId = null, Guid? recordingId = null, string? sid = null,
            string? errorCode = null) => new()
            {
                Stage = stage,
                Outcome = outcome,
                OccurredAtUtc = at,
                Provider = "Agora",
                RecordingCorrelationId = correlationId,
                RecordingId = recordingId,
                Sid = sid,
                ErrorCode = errorCode
            };

        [Fact]
        public async Task Timeline_stitches_provider_SID_stages_onto_the_canonical_correlation_id()
        {
            var recordingId = Guid.NewGuid();
            var correlationId = RecordingLogContext.ForRecording(recordingId);
            const string sid = "sid-abc";
            var t0 = new DateTime(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

            await using var ctx = NewContext();
            ctx.RecordingPipelineEvents.AddRange(
                // Provider stages: know only the SID, no recording id / correlation id.
                Evt("Acquire", "Succeeded", t0.AddSeconds(0), sid: sid),
                Evt("Start", "Succeeded", t0.AddSeconds(1), sid: sid),
                // Webhook + metadata: know the canonical id.
                Evt("WebhookReceived", "Succeeded", t0.AddSeconds(2), correlationId: correlationId, sid: sid),
                Evt("Stop", "Succeeded", t0.AddSeconds(3), sid: sid),
                Evt("MetadataSaved", "Succeeded", t0.AddSeconds(4), correlationId: correlationId, recordingId: recordingId));
            await ctx.SaveChangesAsync();

            var store = new SqlRecordingTimelineStore(ctx);

            var timeline = await store.GetTimelineAsync(
                correlationId,
                new RecordingTimelineJoinKeys { RecordingId = recordingId, Sid = sid });

            // All five stages appear on one timeline, in order, despite three of
            // them never carrying the canonical id.
            Assert.Equal(5, timeline.Entries.Count);
            Assert.Equal(
                new[] { "Acquire", "Start", "WebhookReceived", "Stop", "MetadataSaved" },
                timeline.Entries.Select(e => e.Stage).ToArray());
            Assert.Equal("MetadataSaved", timeline.FurthestStageReached);
            Assert.Equal("Healthy", timeline.Verdict);
        }

        [Fact]
        public async Task Verdict_names_the_failed_stage()
        {
            const string sid = "sid-fail";
            var t0 = new DateTime(2026, 7, 23, 11, 0, 0, DateTimeKind.Utc);

            await using var ctx = NewContext();
            ctx.RecordingPipelineEvents.AddRange(
                Evt("Acquire", "Succeeded", t0.AddSeconds(0), sid: sid),
                Evt("Start", "Succeeded", t0.AddSeconds(1), sid: sid),
                Evt("Stop", "Failed", t0.AddSeconds(2), sid: sid, errorCode: "Agora.Stop.NoFilesProduced"));
            await ctx.SaveChangesAsync();

            var store = new SqlRecordingTimelineStore(ctx);
            var timeline = await store.GetTimelineAsync(
                "rec-whatever", new RecordingTimelineJoinKeys { Sid = sid });

            Assert.Equal("Failed@Stop", timeline.Verdict);
            Assert.Equal("Stop", timeline.FailedStage);
        }

        [Fact]
        public async Task Unknown_correlation_id_yields_an_empty_NoData_timeline()
        {
            await using var ctx = NewContext();
            var store = new SqlRecordingTimelineStore(ctx);

            var timeline = await store.GetTimelineAsync(
                "rec-missing", new RecordingTimelineJoinKeys { Sid = "nope" });

            Assert.Empty(timeline.Entries);
            Assert.Equal("NoData", timeline.Verdict);
        }
    }
}
