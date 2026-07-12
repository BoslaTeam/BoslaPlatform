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
    private readonly Mock<IAgoraRecordingDownloader> _downloaderMock;
    private readonly Mock<IRecordingStorageSettings> _storageSettingsMock;
    private readonly RecordingTransferService _service;

    public RecordingTransferServiceTests()
    {
        _providerMock = new Mock<IRecordingProvider>();
        _storageMock = new Mock<IObjectStorage>();
        _downloaderMock = new Mock<IAgoraRecordingDownloader>();
        _storageSettingsMock = new Mock<IRecordingStorageSettings>();
        var loggerMock = Mock.Of<ILogger<RecordingTransferService>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RecordingTransferTest_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);

        _storageSettingsMock.SetupGet(x => x.BucketName).Returns("recordings");
        _storageSettingsMock.SetupGet(x => x.Provider).Returns(CloudflareR2);
        _storageSettingsMock.SetupGet(x => x.MaxRetryAttempts).Returns(1);
        _storageSettingsMock.SetupGet(x => x.RetryBaseDelaySeconds).Returns(0);

        _downloaderMock.Setup(x => x.DownloadAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<RecordingFileInfo>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, string _, string _, RecordingFileInfo file, int index, CancellationToken _) =>
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"bosla_test_{Guid.NewGuid():N}_{index}_{file.FileName}");
                File.WriteAllBytes(tempPath, new byte[Math.Max(1, (int)file.FileSize)]);
                return Task.FromResult<Result<AgoraRecordingDownloadResult>>(
                    new AgoraRecordingDownloadResult(tempPath, file.FileName, file.MimeType, new FileInfo(tempPath).Length));
            });

        _storageMock.Setup(x => x.ExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new RecordingTransferService(
            _providerMock.Object,
            _storageMock.Object,
            _downloaderMock.Object,
            _storageSettingsMock.Object,
            Mock.Of<IRecordingMetrics>(),
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
        Assert.Contains(session.DomainEvents, e => e is RecordingUploadFailedEvent);
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

        SetupQueryResult(files);
        SetupSuccessfulUpload();

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Uploaded, session.UploadStatus);
        Assert.Equal(CloudflareR2, session.StorageProvider);
        Assert.Equal("recordings", session.BucketName);
        Assert.Equal(1024L, session.ContentLength);
        Assert.Equal("application/vnd.apple.mpegurl", session.ContentType);
        Assert.Contains(session.DomainEvents, e => e is RecordingUploadedEvent);
    }

    [Fact]
    public async Task TransferRecordingAsync_upload_fails_marks_failed()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = SingleFile(session);
        SetupQueryResult(files);

        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadObjectResponse>.Failure(
                Error.Failure("Storage.Error", "Upload rejected")));

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Contains("Upload rejected", session.LastUploadError);
        Assert.Contains(session.DomainEvents, e => e is RecordingUploadFailedEvent);
    }

    [Fact]
    public async Task TransferRecordingAsync_verification_false_marks_failed()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        SetupQueryResult(SingleFile(session));
        SetupSuccessfulUpload();
        _storageMock.Setup(x => x.ExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Contains("not found", session.LastUploadError);
    }

    [Fact]
    public async Task TransferRecordingAsync_verifies_each_uploaded_file()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = new List<RecordingFileInfo>
        {
            new("playlist.m3u8", "playlist.m3u8", 100L, null, "application/vnd.apple.mpegurl", "https://download/playlist.m3u8"),
            new("segment.ts", "segment.ts", 200L, null, "video/mp2t", "https://download/segment.ts")
        };
        SetupQueryResult(files);
        SetupSuccessfulUpload();

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        _storageMock.Verify(x => x.ExistsAsync(
            "recordings",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TransferRecordingAsync_selects_mp4_as_playback_file()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        var files = new List<RecordingFileInfo>
        {
            new("playlist.m3u8", "playlist.m3u8", 100L, null, "application/vnd.apple.mpegurl", "https://download/playlist.m3u8"),
            new("segment.ts", "segment.ts", 200L, null, "video/mp2t", "https://download/segment.ts"),
            new("recording.mp4", "recording.mp4", 300L, null, "video/mp4", "https://download/recording.mp4")
        };
        SetupQueryResult(files);
        SetupSuccessfulUpload();

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal($"{session.Id}/recording.mp4", session.ObjectKey);
        Assert.Equal("video/mp4", session.ContentType);
        Assert.Equal(300L, session.ContentLength);
    }

    [Fact]
    public async Task TransferRecordingAsync_upload_retry_succeeds()
    {
        var session = CreateSavedSession();
        _context.VideoSessions.Add(session);
        _context.SaveChanges();

        _storageSettingsMock.SetupGet(x => x.MaxRetryAttempts).Returns(2);
        SetupQueryResult(SingleFile(session));

        var callCount = 0;
        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((UploadObjectRequest request, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromResult<Result<UploadObjectResponse>>(
                        Error.Failure("Storage.Transient", "Transient upload failure"));
                }

                return Task.FromResult<Result<UploadObjectResponse>>(
                    new UploadObjectResponse(
                        request.BucketName,
                        request.ObjectKey,
                        request.ContentLength,
                        DateTime.UtcNow));
            });

        await _service.TransferRecordingAsync(
            session.Id, "resource-1", "sid-1", CancellationToken.None);

        Assert.Equal(2, callCount);
        Assert.Equal(UploadStatus.Uploaded, session.UploadStatus);
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
        SetupQueryResult(files);

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

    private void SetupQueryResult(IReadOnlyList<RecordingFileInfo> files)
    {
        var queryResult = new QueryResult(
            RecordingStatus.Completed, "resource-1", "sid-1", files,
            new RecordingSummary(files.Count, files.Sum(x => x.FileSize)));

        _providerMock.Setup(x => x.QueryAsync("resource-1", "sid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);
    }

    private void SetupSuccessfulUpload()
    {
        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UploadObjectRequest request, CancellationToken _) =>
                new UploadObjectResponse(
                    request.BucketName,
                    request.ObjectKey,
                    request.ContentLength,
                    DateTime.UtcNow,
                    ETag: "\"abc123\""));
    }

    private static List<RecordingFileInfo> SingleFile(VideoSession session)
    {
        return
        [
            new("recording.m3u8", $"{session.Id}/recording.m3u8", 1024L, null,
                "application/vnd.apple.mpegurl",
                "https://test.download/recording.m3u8")
        ];
    }

    private static VideoSession CreateSavedSession()
    {
        var result = VideoSession.Create(
            Guid.NewGuid(), "channel-test", "app-id", VideoSessionType.OneToOne);
        return result.Value;
    }
}
