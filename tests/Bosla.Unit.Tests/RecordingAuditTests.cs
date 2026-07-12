using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Entities.System;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingAuditTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly RecordingAuditService _service;

    public RecordingAuditTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuditTest_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);
        _service = new RecordingAuditService(_context, Mock.Of<ILogger<RecordingAuditService>>());
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task LogAsync_persists_audit_record_with_correct_fields()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow.AddSeconds(-1);

        await _service.LogAsync(sessionId, userId, RecordingAuditAction.Viewed);

        var log = await _context.RecordingAuditLogs
            .FirstOrDefaultAsync(l => l.VideoSessionId == sessionId);

        Assert.NotNull(log);
        Assert.Equal(sessionId, log.VideoSessionId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(RecordingAuditAction.Viewed, log.Action);
        Assert.True(log.OccurredAtUtc >= before);
    }

    [Fact]
    public async Task LogAsync_allows_null_userId_for_system_actions()
    {
        var sessionId = Guid.NewGuid();

        await _service.LogAsync(sessionId, null, RecordingAuditAction.UploadCompleted);

        var log = await _context.RecordingAuditLogs
            .FirstOrDefaultAsync(l => l.VideoSessionId == sessionId);

        Assert.NotNull(log);
        Assert.Null(log.UserId);
        Assert.Equal(RecordingAuditAction.UploadCompleted, log.Action);
    }

    [Fact]
    public async Task LogAsync_creates_distinct_entries_for_multiple_calls()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _service.LogAsync(sessionId, userId, RecordingAuditAction.Viewed);
        await _service.LogAsync(sessionId, userId, RecordingAuditAction.Downloaded);

        var logs = await _context.RecordingAuditLogs
            .Where(l => l.VideoSessionId == sessionId)
            .ToListAsync();

        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Action == RecordingAuditAction.Viewed);
        Assert.Contains(logs, l => l.Action == RecordingAuditAction.Downloaded);
    }

    [Fact]
    public async Task RecordingAuditLog_Create_never_stores_sensitive_data()
    {
        var log = RecordingAuditLog.Create(Guid.NewGuid(), Guid.NewGuid(), RecordingAuditAction.Viewed);

        // Verify: no properties store URL-like strings
        var props = typeof(RecordingAuditLog).GetProperties();
        foreach (var prop in props)
        {
            var value = prop.GetValue(log)?.ToString() ?? string.Empty;
            Assert.False(
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
                $"Property {prop.Name} must not contain a URL");
        }
    }

    [Fact]
    public async Task LogAsync_does_not_throw_on_database_failure()
    {
        // Arrange: use a disposed context to simulate a DB failure
        var badOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BadDb_{Guid.NewGuid()}")
            .Options;
        var badContext = new AppDbContext(badOptions);
        badContext.Dispose(); // Force failure

        var service = new RecordingAuditService(badContext, Mock.Of<ILogger<RecordingAuditService>>());

        // Act + Assert: must not throw
        var ex = await Record.ExceptionAsync(
            () => service.LogAsync(Guid.NewGuid(), null, RecordingAuditAction.UploadFailed));

        Assert.Null(ex);
    }
}
