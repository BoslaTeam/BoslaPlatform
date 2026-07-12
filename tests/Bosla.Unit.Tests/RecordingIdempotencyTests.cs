using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingIdempotencyTests
{
    // ── RecordingIntegrityVerifier ─────────────────────────────────────────────

    [Fact]
    public async Task IntegrityVerifier_passes_when_ContentLength_matches()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.GetMetadataAsync("bucket", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectMetadata(
                BucketName: "bucket",
                ObjectKey: "key",
                ContentType: "video/mp4",
                ContentLength: 1024,
                LastModified: DateTime.UtcNow));

        var verifier = new RecordingIntegrityVerifier(
            storageMock.Object,
            Mock.Of<ILogger<RecordingIntegrityVerifier>>());

        var uploadResponse = new UploadObjectResponse(
            BucketName: "bucket",
            ObjectKey: "key",
            ContentLength: 1024,
            UploadedAtUtc: DateTime.UtcNow);

        var result = await verifier.VerifyAsync("bucket", "key", uploadResponse);

        Assert.False(result.IsError);
    }

    [Fact]
    public async Task IntegrityVerifier_fails_when_ContentLength_mismatches()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.GetMetadataAsync("bucket", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectMetadata(
                BucketName: "bucket",
                ObjectKey: "key",
                ContentType: "video/mp4",
                ContentLength: 999,  // Different from upload
                LastModified: DateTime.UtcNow));

        var verifier = new RecordingIntegrityVerifier(
            storageMock.Object,
            Mock.Of<ILogger<RecordingIntegrityVerifier>>());

        var uploadResponse = new UploadObjectResponse(
            BucketName: "bucket",
            ObjectKey: "key",
            ContentLength: 1024, // Expected 1024
            UploadedAtUtc: DateTime.UtcNow);

        var result = await verifier.VerifyAsync("bucket", "key", uploadResponse);

        Assert.True(result.IsError);
        Assert.Contains("ContentLength mismatch", result.Errors[0].Description);
    }

    [Fact]
    public async Task IntegrityVerifier_fails_when_metadata_unavailable()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.GetMetadataAsync("bucket", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("R2.NotFound", "Object not found"));

        var verifier = new RecordingIntegrityVerifier(
            storageMock.Object,
            Mock.Of<ILogger<RecordingIntegrityVerifier>>());

        var uploadResponse = new UploadObjectResponse(
            BucketName: "bucket",
            ObjectKey: "key",
            ContentLength: 1024,
            UploadedAtUtc: DateTime.UtcNow);

        var result = await verifier.VerifyAsync("bucket", "key", uploadResponse);

        Assert.True(result.IsError);
        Assert.Contains("IntegrityFailed", result.Errors[0].Code);
    }

    [Fact]
    public async Task IntegrityVerifier_passes_with_etag_match_in_metadata_dict()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.GetMetadataAsync("bucket", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectMetadata(
                BucketName: "bucket",
                ObjectKey: "key",
                ContentType: "video/mp4",
                ContentLength: 512,
                LastModified: DateTime.UtcNow,
                Metadata: new Dictionary<string, string> { ["etag"] = "\"abc123\"" }));

        var verifier = new RecordingIntegrityVerifier(
            storageMock.Object,
            Mock.Of<ILogger<RecordingIntegrityVerifier>>());

        // Upload response ETag without quotes — normalisation must match
        var uploadResponse = new UploadObjectResponse(
            BucketName: "bucket",
            ObjectKey: "key",
            ContentLength: 512,
            UploadedAtUtc: DateTime.UtcNow,
            ETag: "abc123");

        var result = await verifier.VerifyAsync("bucket", "key", uploadResponse);

        Assert.False(result.IsError);
    }

    // ── RecordingFailureClassifier ─────────────────────────────────────────────

    [Fact]
    public void FailureClassifier_classifies_HttpRequestException_as_Network()
    {
        var category = RecordingFailureClassifier.Classify(new HttpRequestException("Connection reset"));
        Assert.Equal(BoslaPlatform.Domain.Enums.RecordingFailureCategory.Network, category);
    }

    [Fact]
    public void FailureClassifier_classifies_OperationCanceledException_as_Transient()
    {
        var category = RecordingFailureClassifier.Classify(new OperationCanceledException());
        Assert.Equal(BoslaPlatform.Domain.Enums.RecordingFailureCategory.Transient, category);
    }

    [Fact]
    public void FailureClassifier_classifies_generic_exception_as_Transient()
    {
        // Conservative default — any unrecognised exception is treated as transient
        var category = RecordingFailureClassifier.Classify(new InvalidOperationException("unexpected"));
        Assert.Equal(BoslaPlatform.Domain.Enums.RecordingFailureCategory.Transient, category);
    }

    [Fact]
    public void FailureClassifier_IsRetriable_true_for_Transient()
        => Assert.True(RecordingFailureClassifier.IsRetriable(
            BoslaPlatform.Domain.Enums.RecordingFailureCategory.Transient));

    [Fact]
    public void FailureClassifier_IsRetriable_true_for_Network()
        => Assert.True(RecordingFailureClassifier.IsRetriable(
            BoslaPlatform.Domain.Enums.RecordingFailureCategory.Network));

    [Fact]
    public void FailureClassifier_IsRetriable_false_for_Permanent()
        => Assert.False(RecordingFailureClassifier.IsRetriable(
            BoslaPlatform.Domain.Enums.RecordingFailureCategory.Permanent));

    [Fact]
    public void FailureClassifier_IsRetriable_false_for_Authentication()
        => Assert.False(RecordingFailureClassifier.IsRetriable(
            BoslaPlatform.Domain.Enums.RecordingFailureCategory.Authentication));

    [Fact]
    public void FailureClassifier_IsRetriable_false_for_Storage()
        => Assert.False(RecordingFailureClassifier.IsRetriable(
            BoslaPlatform.Domain.Enums.RecordingFailureCategory.Storage));
}
