# Media Storage Pipeline

## Architecture

```
RecordingUploadRequestedEvent
         │
         ▼
RecordingUploadRequestedEventHandler
         │
         ▼
RecordingTransferService (Application)
   ├─ Query Agora via IRecordingProvider
   ├─ Download recording files to temp
   ├─ Upload to Cloudflare R2 via IObjectStorage
   ├─ Clean up temp files
   ├─ Update VideoSession entity state
   └─ Publish domain events (success/failure)
         │
         ▼
RecordingUploadedEvent / RecordingUploadFailedEvent
```

## Sequence

```
 Handler         TransferService    IRecordingProvider    IObjectStorage      VideoSession
   │                    │                    │                    │                 │
   │──Handle()─────────►│                    │                    │                 │
   │                    │──MarkUploadPending()│                    │────────────────►│
   │                    │──QueryAsync()──────►│                    │                 │
   │                    │◄────QueryResult─────│                    │                 │
   │                    │                    │                    │                 │
   │                    │  for each file:    │                    │                 │
   │                    │──MarkUploading()───│────────────────────│────────────────►│
   │                    │──DownloadFile()    │                    │                 │
   │                    │──UploadAsync()─────────────────────────►│                 │
   │                    │◄─UploadObjectResp──│                    │                 │
   │                    │                    │                    │                 │
   │                    │──MarkUploadSucceeded()─────────────────►│                 │
   │                    │──AddDomainEvent()  │                    │                 │
   │                    │◄───────────────────────────────────────│                 │
```

## Object Storage Abstraction

`IObjectStorage` is provider-agnostic. Implementations:

| Provider | Class | Notes |
|----------|-------|-------|
| Cloudflare R2 | `CloudflareR2ObjectStorage` | AWS SDK S3-compatible |
| Amazon S3 | (future) | Swap via `StorageOptions.Provider` |
| Azure Blob | (future) | Same interface |
| Google Cloud Storage | (future) | Same interface |
| MinIO | (future) | Same interface |

### Configuration (`StorageOptions`)

```json
{
  "Storage": {
    "Provider": "CloudflareR2",
    "AccessKey": "your-r2-access-key",
    "SecretKey": "your-r2-secret-key",
    "ServiceUrl": "https://<account-id>.r2.cloudflarestorage.com",
    "BucketName": "recordings",
    "PresignedUrlExpirationMinutes": 15,
    "MaxRetryAttempts": 3,
    "RetryBaseDelaySeconds": 2
  }
}
```

## Domain Events

| Event | When Published | Payload |
|-------|---------------|---------|
| `RecordingUploadedEvent` | All files uploaded to R2 | `SessionId`, `ObjectKey`, `BucketName`, `ContentLength`, `UploadDuration` |
| `RecordingUploadFailedEvent` | Any upload error | `SessionId`, `FailureReason`, `Attempts` |

## Entity State (`VideoSession`)

| Field | Type | Description |
|-------|------|-------------|
| `UploadStatus` | `UploadStatus` enum | `Pending`, `Uploading`, `Uploaded`, `Failed`, `Retrying`, `Cancelled` |
| `StorageProvider` | `string?` | `"CloudflareR2"`, `"AwsS3"`, etc. |
| `BucketName` | `string?` | R2 bucket name |
| `ObjectKey` | `string?` | Object key in bucket |
| `ContentType` | `string?` | MIME type |
| `ContentLength` | `long?` | Total bytes uploaded |
| `UploadedAtUtc` | `DateTime?` | When upload completed |
| `UploadAttempts` | `int` | Number of upload attempts |
| `LastUploadError` | `string?` | Last error message |

## Resilience

- Cloudflare R2: AWS SDK adaptive retry mode (up to `MaxRetryAttempts`)
- Never retry **401 Unauthorized**, **403 Forbidden**, **404 Not Found**, **400 Bad Request**
- Download: `HttpClient` with 5-minute timeout
- Temp files deleted in `finally` block

## Security

- Access key / secret key: loaded via `IOptions<StorageOptions>` (never logged)
- Presigned URLs: time-limited (default 15 min), `GET` verb only
- No public URLs exposed — always use `GeneratePresignedUrlAsync()`
- Temp files stored in `Path.GetTempPath()`, deleted immediately after upload
- Structured logging includes: `VideoSessionId`, `ObjectKey`, `Bucket`, `UploadDuration`, `ContentLength`
- Never log: `AccessKey`, `SecretKey`, full presigned URL

## Configuration Validation

`StorageOptions` uses `[Required]` data annotations validated on startup via `ValidateOnStart()`.
