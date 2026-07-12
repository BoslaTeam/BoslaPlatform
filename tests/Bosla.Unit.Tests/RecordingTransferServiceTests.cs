using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Features.RecordingTransfer.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BoslaPlatform.Domain.Enums.StorageProvider;

namespace Bosla.Unit.Tests;

public class RecordingTransferServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IRecordingProvider> _providerMock;
    private readonly Mock<IObjectStorage> _storageMock;
    private readonly Mock<IFileDownloader> _downloaderMock;
    private readonly RecordingTransferService _service;

    public RecordingTransferServiceTests()
    {
        _providerMock = new Mock<IRecordingProvider>();
        _storageMock = new Mock<IObjectStorage>();
        _downloaderMock = new Mock<IFileDownloader>();
        var loggerMock = Mock.Of<ILogger<RecordingTransferService>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RecordingTransferTest_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);

        _downloaderMock.Setup(x => x.DownloadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string destPath, CancellationToken _) =>
            {
                File.WriteAllBytes(destPath, "test content"u8.ToArray());
                return Task.CompletedTask;
            });

        _service = new RecordingTransferService(
            _providerMock.Object,
            _storageMock.Object,
            _downloaderMock.Object,
            _context,
            loggerMock);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task TransferRecordingAsync_session_not_found_logs_warning()
    {
        await _service.TransferRecordingAsync(
            Guid.NewGuid(), "resource-1", "sid-1", CancellationToken.None);

        _providerMock.Verify(
            x => x.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransferRecordingAsync_provider_error_marks_failed()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryResult>.Failure(
                Error.Failure("Provider.Error", "Failed to query")));

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.True(session.DomainEvents.Any(e => e is RecordingUploadFailedEvent));
    }

    [Fact]
    public async Task TransferRecordingAsync_no_files_marks_failed()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var queryResult = new QueryResult(
            RecordingStatus.Completed, "resource-1", "sid-1", null, null);

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Contains("No recording files", session.LastUploadError);
    }

    [Fact]
    public async Task TransferRecordingAsync_upload_success_publishes_uploaded_event()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = new List<RecordingFileInfo>
        {
            new("recording.m3u8", $"{session.Id}/recording.m3u8", 1024L, null,
                "application/vnd.apple.mpegurl",
                "https://test.download/recording.m3u8")
        };

        var queryResult = new QueryResult(
            RecordingStatus.Completed, "resource-1", "sid-1", files,
            new RecordingSummary(1, 1024L));

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadObjectResponse(
                "recordings",
                $"{session.Id}/recording.m3u8",
                1024L,
                DateTime.UtcNow,
                ETag: "\"abc123\""));

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Uploaded, session.UploadStatus);
        Assert.Equal(CloudflareR2, session.StorageProvider);
        Assert.Equal("recordings", session.BucketName);
        Assert.Equal(1024L, session.ContentLength);
        Assert.True(session.DomainEvents.Any(e => e is RecordingUploadedEvent));
    }

    [Fact]
    public async Task TransferRecordingAsync_upload_fails_marks_failed()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = new List<RecordingFileInfo>
        {
            new("recording.m3u8", $"{session.Id}/recording.m3u8", 1024L, null,
                "application/vnd.apple.mpegurl",
                "https://test.download/recording.m3u8")
        };

        var queryResult = new QueryResult(
            RecordingStatus.Completed, "resource-1", "sid-1", files,
            new RecordingSummary(1, 1024L));

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadObjectResponse>.Failure(
                Error.Failure("Storage.Error", "Upload rejected")));

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Contains("Upload rejected", session.LastUploadError);
        Assert.True(session.DomainEvents.Any(e => e is RecordingUploadFailedEvent));
    }

    [Fact]
    public async Task TransferRecordingAsync_first_file_fails_does_not_process_remaining()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = new List<RecordingFileInfo>
        {
            new("file1.m3u8", $"{session.Id}/file1.m3u8", 100L, null, "text/plain",
                "https://test.download/file1.m3u8"),
            new("file2.ts", $"{session.Id}/file2.ts", 200L, null, "text/plain",
                "https://test.download/file2.ts")
        };

        var queryResult = new QueryResult(
            RecordingStatus.Completed, "resource-1", "sid-1", files,
            new RecordingSummary(2, 300L));

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        var callCount = 0;
        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromResult<Result<UploadObjectResponse>>(
                        Error.Failure("Storage.Error", "Upload rejected"));
                }
                return Task.FromResult<Result<UploadObjectResponse>>(
                    new UploadObjectResponse("b", "k", 0, DateTime.UtcNow));
            });

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(1, callCount);
        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
    }

    private static VideoSession CreateSavedSession()
    {
        var result = VideoSession.Create(
            Guid.NewGuid(), "channel-test", "app-id", VideoSessionType.OneToOne);
        return result.Value;
    }
}