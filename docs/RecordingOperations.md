# Recording Operations Guide

> **Project:** BoslaPlatform  
> **Layer:** Infrastructure / Recording  
> **Last updated:** 2026-07-12

---

## Overview

This document covers the production hardening additions to the Bosla recording pipeline.  
All features are **additive** — no existing public APIs were changed.

---

## Architecture: Recording Pipeline

```
Agora Cloud Recording
        │
        ▼
 AgoraRecordingProvider
        │  QueryAsync / StopAsync
        ▼
 RecordingTransferService        ◄── RecordingReconciliationService (background)
        │  Download → Upload → Verify
        ▼
 CloudflareR2ObjectStorage  (IObjectStorage)
        │
        ▼
  VideoSession (aggregate root, EF Core / SQL Server)
        │
        ▼
  RecordingAuditLog (append-only table)
```

---

## 1. Recording Reconciliation Job

**Class:** `RecordingReconciliationService` (BackgroundService)  
**Config section:** `RecordingReconciliation`

### What it does
Polls the database every `PollingIntervalSeconds` seconds for recordings stuck in `Pending` or `Retrying` states. For each eligible recording it:

1. Acquires a per-session lock (`IRecordingLock`).
2. Queries Agora for recording file status.
3. If files are ready → calls `RecordingTransferService.TransferRecordingAsync`.
4. If files are not ready → schedules an exponential-backoff retry.
5. If `RetryCount ≥ MaxRetryAttempts` → marks the recording as `Cancelled`.

### Configuration

```json
"RecordingReconciliation": {
  "MaxRetryAttempts": 5,
  "BaseBackoffSeconds": 60,
  "PollingIntervalSeconds": 300,
  "BatchSize": 20
}
```

### Exponential Backoff Formula
```
nextRetryDelay = BaseBackoffSeconds × 2^(RetryCount)
```
| Attempt | Delay (BaseBackoff=60s) |
|---------|------------------------|
| 1       | 60s (1 min)            |
| 2       | 120s (2 min)           |
| 3       | 240s (4 min)           |
| 4       | 480s (8 min)           |
| 5       | 960s (16 min)          |

---

## 2. Upload Idempotency

**Guard location:** `RecordingTransferService.TransferRecordingAsync`

Before downloading any files, the service checks whether the recording object already exists in R2 via `IObjectStorage.ExistsAsync`. If the object already exists **and** the session is already in `Uploaded` state, the transfer is skipped.

This prevents double-uploads caused by:
- Duplicate event messages
- Reconciliation triggering on an already-completed session

---

## 3. Integrity Verification

**Class:** `RecordingIntegrityVerifier`

After every upload, the service fetches object metadata from R2 and verifies:

| Check | Failure Response |
|-------|-----------------|
| `ContentLength` matches upload response | `Recording.IntegrityFailed` |
| `ETag` matches upload response (quote-normalised) | `Recording.IntegrityFailed` |
| Object exists (GetMetadata succeeds) | `Recording.IntegrityFailed` |

ETags from S3-compatible APIs are normalised by stripping surrounding quotes before comparison.

---

## 4. Recording Expiration Policy

**Status:** Architecture stub — deletion is NOT yet implemented.

Two new columns on `VideoSessions`:

| Column | Purpose |
|--------|---------|
| `ExpiresAtUtc` | Set by `MarkExpired()` when the retention window passes |
| `DeletedAtUtc` | Set by `MarkSoftDeleted()` — physical file NOT removed |

**Configuration:**

```json
"RecordingRetention": {
  "RetentionDays": 365,
  "EnableSoftDelete": false,
  "EnableHardDelete": false
}
```

> ⚠️ Both delete flags are `false` by default. A future `RetentionCleanupService` will implement actual deletion.

---

## 5. Storage Metrics

**Interface:** `IRecordingMetrics`  
**Default implementation:** `NoOpRecordingMetrics` (no-op, zero overhead)

| Method | When Called |
|--------|-------------|
| `RecordUploadDuration(TimeSpan)` | After successful upload |
| `RecordUploadSuccess(bytes, duration)` | After successful upload |
| `RecordUploadFailure(errorCode)` | On any upload failure |
| `RecordRetry()` | On each retry |
| `IncrementActiveUploads()` | On transfer start |
| `DecrementActiveUploads()` | On transfer end (finally block) |
| `RecordPresignedUrlGenerated()` | On presigned URL generation |
| `RecordDownloadDuration(TimeSpan)` | After download stream opened |
| `RecordAverageRecordingSize(bytes)` | After successful upload |
| `RecordPendingUploads(count)` | Per reconciliation pass |

To enable real metrics, implement `IRecordingMetrics` with OpenTelemetry `Meter` and register it in DI as a Singleton.

---

## 6. Health Checks

Two health checks are registered under the `storage` tag:

### `storage-configuration` (startup)
Validates all required `StorageOptions` fields. **No network calls.** Fast, suitable for startup probes.

### `cloudflare-r2` (readiness)
Issues an `ExistsAsync` call to R2 with a known sentinel key (`__healthcheck__`). A 404 response is treated as **Healthy** (proves connectivity). Times out after 5 seconds.

```json
// appsettings.json — health endpoint exposed by ASP.NET Core
"HealthChecks": {
  "Port": 8081
}
```

Standard `/health` and `/health/ready` endpoints (configure in `Program.cs` as needed).

---

## 7. Audit Logging

**Interface:** `IRecordingAuditService`  
**Table:** `RecordingAuditLogs`

Every access to a recording is logged immutably. The log contains:

| Column | Value |
|--------|-------|
| `VideoSessionId` | The accessed session |
| `UserId` | The requesting user (null for system) |
| `Action` | `Viewed`, `Downloaded`, `UploadCompleted`, `UploadFailed`, `Deleted` |
| `OccurredAtUtc` | Server UTC time |

> **Security:** Presigned URLs are **never** stored in the audit log.

Audit failures are **silently logged** — they never surface to the caller. Recording access is never blocked by an audit error.

---

## 8. Temporary File Cleanup

**Class:** `DefaultTemporaryFileCleaner`  
**Config section:** `TemporaryFileCleaner`

Cleanup now operates in two categories:

| Category | Pattern | What It Removes |
|----------|---------|-----------------|
| Orphan downloads | `bosla_*` | Download temp files left by failed/interrupted transfers |
| Generic remnants | `*.tmp`, `*.temp` | Any temp files older than the retention window |

Files larger than **100 MB** are skipped with a warning (safety guard).

```json
"TemporaryFileCleaner": {
  "RetentionMinutes": 60,
  "PollingIntervalSeconds": 300,
  "BatchSize": 100
}
```

---

## 9. Secure Watch URL Improvements

`RecordingAccessService.GetWatchUrlAsync` now:

1. **Clamps expiration** — the requested TTL is capped to `StorageOptions.PresignedUrlExpirationMinutes`. A caller passing `TimeSpan.MaxValue` will receive a URL that expires at the configured maximum.
2. **Guards failed uploads** — returns `Recording.NotFound` if `UploadStatus != Uploaded`.
3. **Emits audit** — logs `RecordingAuditAction.Viewed` after URL generation.
4. **Emits metric** — calls `RecordPresignedUrlGenerated()`.

---

## 10. Concurrency Protection

### Two-Layer Guard

| Layer | Mechanism | Scope |
|-------|-----------|-------|
| Process-level | `OptimisticRecordingLock` (ConcurrentDictionary) | Same application pod |
| Cross-process | EF Core `RowVersion` (SQL Server rowversion) | Multiple pods |

`OptimisticRecordingLock.TryAcquireAsync` is atomic — only one task can acquire the lock per session. If a `DbUpdateConcurrencyException` is thrown on save, the reconciliation job treats it as "another pod won" and silently skips the session.

For multi-pod deployments where in-memory locks are insufficient, replace `OptimisticRecordingLock` with a Redis-backed implementation of `IRecordingLock`. EF RowVersion remains as the final guard regardless.

---

## 11. Failure Recovery

### Failure Categories

| Category | Retriable | Examples |
|----------|-----------|---------|
| `Transient` | ✅ | HTTP 429, 503, OperationCanceledException |
| `Network` | ✅ | HttpRequestException, DNS failure |
| `Authentication` | ❌ | HTTP 401, 403 |
| `Permanent` | ❌ | HTTP 400, 404, bad object key |
| `Storage` | ❌ | Bucket misconfigured, quota exceeded |

The `RecordingFailureClassifier` classifies every `Exception` and maps it to a category. Non-retriable failures immediately set `UploadStatus = Failed` without further retry attempts.

---

## 12. Database Migration

Run after deploying:

```bash
dotnet ef migrations add HardeningV1 \
  --project BoslaPlatform.Infrastructure \
  --startup-project BoslaPlatform.API

dotnet ef database update \
  --project BoslaPlatform.Infrastructure \
  --startup-project BoslaPlatform.API
```

### New Columns on `VideoSessions`

| Column | Type | Default |
|--------|------|---------|
| `RetryCount` | int | 0 |
| `LastRetryAtUtc` | datetime2 | NULL |
| `NextRetryAtUtc` | datetime2 | NULL |
| `FailureCategory` | nvarchar(30) | NULL |
| `ExpiresAtUtc` | datetime2 | NULL |
| `DeletedAtUtc` | datetime2 | NULL |
| `RowVersion` | rowversion | auto |

### New Table: `RecordingAuditLogs`

| Column | Type |
|--------|------|
| `Id` | uniqueidentifier PK |
| `VideoSessionId` | uniqueidentifier FK (Restrict) |
| `UserId` | uniqueidentifier NULL |
| `Action` | nvarchar(30) NOT NULL |
| `OccurredAtUtc` | datetime2 NOT NULL |

### New Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| `VideoSessions` | `IX_VideoSessions_UploadStatus_NextRetryAtUtc` | Reconciliation batch query |
| `RecordingAuditLogs` | `IX_RecordingAuditLogs_VideoSessionId` | Session audit lookup |
| `RecordingAuditLogs` | `IX_RecordingAuditLogs_OccurredAtUtc` | Time-range reporting |

---

## 13. Running Tests

```bash
dotnet test tests/Bosla.Unit.Tests/
```

New test files added:

| File | Coverage |
|------|---------|
| `RecordingReconciliationTests.cs` | Reconciliation pass, lock, backoff, max-retry |
| `RecordingIdempotencyTests.cs` | IntegrityVerifier, FailureClassifier |
| `RecordingAuditTests.cs` | Audit persistence, no-URL contract, DB failure resilience |
| `RecordingHealthCheckTests.cs` | Both health checks — all cases |
| `RecordingConcurrencyTests.cs` | Lock acquire/release/concurrent stress test |

---

## Operational Runbook

### A recording is stuck in `Pending` for > 10 minutes

1. Check `RecordingReconciliationService` logs for errors.
2. Check `AgoraSettings:CloudRecordingBaseUrl` is reachable (Agora health check).
3. Check `/health` endpoint — `cloudflare-r2` and `storage-configuration` must be Healthy.
4. If `RetryCount = MaxRetryAttempts` and `UploadStatus = Cancelled`, the recording must be manually re-queued or retrieved from Agora's temporary storage.

### Presigned URL is not working

1. Check `StorageOptions:PresignedUrlExpirationMinutes` — value must match R2 bucket CORS/policy settings.
2. Verify `UploadStatus = Uploaded` for the session.
3. Check audit logs for `Viewed` entries to confirm the URL was generated.

### Storage quota alerts

1. Check `RecordingAuditLogs` for `UploadCompleted` action counts.
2. Consider reducing `RecordingRetention:RetentionDays` and enabling `EnableSoftDelete`.
3. Monitor `RecordPendingUploads` metric.
