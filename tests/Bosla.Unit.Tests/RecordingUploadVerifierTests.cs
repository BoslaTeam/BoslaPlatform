using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

/// <summary>
/// STEP 13–15 verification: a recording only becomes Verified once Agora reports
/// the upload AND S3 HeadObject confirms a non-empty object. Everything else maps
/// to Pending / UploadFailed / VerificationFailed — never Verified.
/// </summary>
public class RecordingUploadVerifierTests
{
    private const string ResourceId = "resource-123";
    private const string Sid = "sid-456";
    private const string Bucket = "bosla-recordings-prod";
    private const string Key = "recordings/appt/xyz.m3u8";

    private readonly Mock<IRecordingProvider> _provider = new();
    private readonly Mock<IRecordingStorage> _storage = new();

    private RecordingUploadVerifier BuildVerifier(int maxAttempts = 1, int baseDelaySeconds = 1)
    {
        var options = Options.Create(new RecordingUploadVerificationOptions
        {
            MaxAttempts = maxAttempts,
            BaseDelaySeconds = baseDelaySeconds
        });

        return new RecordingUploadVerifier(
            _provider.Object,
            _storage.Object,
            options,
            Mock.Of<ILogger<RecordingUploadVerifier>>());
    }

    private void SetupHead(Result<RecordingObjectMetadata> result) =>
        _storage
            .Setup(s => s.HeadObjectAsync(Bucket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public async Task Verified_when_uploaded_and_headobject_confirms_nonempty_object()
    {
        SetupHead(Result<RecordingObjectMetadata>.Success(
            new RecordingObjectMetadata(2048, "application/vnd.apple.mpegurl", "\"etag\"", DateTime.UtcNow, "STANDARD")));

        var verifier = BuildVerifier();

        var result = await verifier.VerifyAsync(
            ResourceId, Sid, Bucket, Key, AgoraUploadingStatus.Uploaded);

        Assert.Equal(UploadVerificationOutcome.Verified, result.Outcome);
        Assert.NotNull(result.Metadata);
        Assert.Equal(2048, result.Metadata!.ContentLength);
        // No query needed — Agora already reported "uploaded".
        _provider.Verify(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadFailed_when_no_object_key()
    {
        var verifier = BuildVerifier();

        var result = await verifier.VerifyAsync(
            ResourceId, Sid, Bucket, objectKey: "", AgoraUploadingStatus.Unknown);

        Assert.Equal(UploadVerificationOutcome.UploadFailed, result.Outcome);
        // Never even touches S3 — there is nothing to look for.
        _storage.Verify(s => s.HeadObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerificationFailed_when_object_exists_but_zero_length()
    {
        SetupHead(Result<RecordingObjectMetadata>.Success(
            new RecordingObjectMetadata(0, "application/octet-stream", null, DateTime.UtcNow, "STANDARD")));

        var verifier = BuildVerifier();

        var result = await verifier.VerifyAsync(
            ResourceId, Sid, Bucket, Key, AgoraUploadingStatus.Uploaded);

        Assert.Equal(UploadVerificationOutcome.VerificationFailed, result.Outcome);
    }

    [Fact]
    public async Task Pending_when_uploaded_but_object_not_found_within_window()
    {
        // Agora says uploaded, but S3 still 404s — object not there yet. With a single
        // attempt the sync window closes and we defer to the async path (PendingUpload).
        SetupHead(Error.NotFound("S3.ObjectNotFound", "not there"));

        var verifier = BuildVerifier(maxAttempts: 1);

        var result = await verifier.VerifyAsync(
            ResourceId, Sid, Bucket, Key, AgoraUploadingStatus.Uploaded);

        Assert.Equal(UploadVerificationOutcome.Pending, result.Outcome);
    }

    [Fact]
    public async Task Polls_query_then_verifies_when_status_flips_to_uploaded()
    {
        // Attempt 1: status is "backuped" (still in Agora backup) → no S3 check, wait, re-query.
        // Query flips to "uploaded"; attempt 2: HeadObject confirms → Verified.
        _provider
            .Setup(p => p.QueryAsync(ResourceId, Sid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult(
                RecordingStatus.Uploaded, ResourceId, Sid,
                UploadingStatus: AgoraUploadingStatus.Uploaded));

        SetupHead(Result<RecordingObjectMetadata>.Success(
            new RecordingObjectMetadata(4096, "application/vnd.apple.mpegurl", "\"e\"", DateTime.UtcNow, "STANDARD")));

        var verifier = BuildVerifier(maxAttempts: 2, baseDelaySeconds: 1);

        var result = await verifier.VerifyAsync(
            ResourceId, Sid, Bucket, Key, AgoraUploadingStatus.Backuped);

        Assert.Equal(UploadVerificationOutcome.Verified, result.Outcome);
        _provider.Verify(p => p.QueryAsync(ResourceId, Sid, It.IsAny<CancellationToken>()), Times.Once);
    }
}
