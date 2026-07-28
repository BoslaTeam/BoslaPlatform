# Recording Lifecycle

## Architecture

The recording system follows a **three-phase orchestration** pattern that keeps domain logic, infrastructure, and external provider concerns cleanly separated.

```
┌────────────────────────────────────────────────────────────────────┐
│                         Controller (API)                           │
│  VideoSessionsController                                          │
│    → StartRecording()  → IVideoSessionService.StartRecordingAsync │
│    → StopRecording()   → IVideoSessionService.StopRecordingAsync  │
│    → QueryStatus()     → IRecordingProvider.QueryAsync            │
└───────────────┬────────────────────────────────────────────────────┘
                │
┌───────────────▼────────────────────────────────────────────────────┐
│                      Service Layer (Application)                   │
│  VideoSessionService                                               │
│    Phase 1: Domain validation + persistence (SQL transaction)      │
│    Phase 2: External provider call   (no transaction)              │
│    Phase 3: Persist provider result  (SQL transaction)             │
│                                                                    │
│  Domain events published via MediatR after each phase.             │
└───────────────┬────────────────────────────────────────────────────┘
                │
┌───────────────▼────────────────────────────────────────────────────┐
│                   IRecordingProvider (Abstraction)                  │
│  AgoraRecordingProvider / NoOpRecordingProvider                    │
│    Orchestrates Acquire → Start / Stop / Query against provider    │
│    Returns strongly-typed result records (QueryResult, etc.)       │
└───────────────┬────────────────────────────────────────────────────┘
                │
┌───────────────▼────────────────────────────────────────────────────┐
│              AgoraCloudRecordingApiClient (Infrastructure)          │
│    Typed HttpClient with:                                          │
│    • Basic Auth (AgoraAuthenticationHandler)                       │
│    • Retry policy (Polly: transient errors + 429)                  │
│    • Structured logging (method, URL, status code, duration)       │
│    • CancellationToken propagation                                 │
└───────────────┬────────────────────────────────────────────────────┘
                │
         Agora REST API (cloud)
```

## Agora REST API Integration

The integration strictly follows the Agora Cloud Recording REST API documentation. A crucial component of the URL path for Start, Stop, and Query actions is the `mode` segment:

**Endpoint Structure:**
- **Acquire:** `/v1/apps/{appId}/cloud_recording/acquire`
- **Start:** `/v1/apps/{appId}/cloud_recording/resourceid/{resourceId}/mode/{mode}/start`
- **Stop:** `/v1/apps/{appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/{mode}/stop`
- **Query:** `/v1/apps/{appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/{mode}/query`

**Recording Modes (`RecordingMode` Enum):**
- **Mix (`mix`)**: Default. Mixes audio and video of all channel users into a single file.
- **Individual (`individual`)**: Records audio and video of each user in separate files.
- **Web (`web`)**: Records the content of a web page.

## Recording Lifecycle State Machine

```
                   ┌──────────────┐
                   │    Idle      │
                   └──────┬───────┘
                          │ StartRecording (by specialist)
                          ▼
              ┌─────────────────────┐
              │     Processing      │  (Acquire → Start via Agora)
              │  RecordingStatus:   │
              │    Processing       │
              └──────────┬──────────┘
                         │ Agora confirms start (webhook 1001 or query)
                         ▼
              ┌─────────────────────┐
              │     Recording       │
              │  RecordingStatus:   │
              │    Recording        │
              └──────────┬──────────┘
                         │ StopRecording (by specialist)
                         ▼
              ┌─────────────────────┐
              │     Stopping        │  (Agora API stop call)
              └──────────┬──────────┘
                         │ Provider responds or webhook 1003
                         ▼
              ┌─────────────────────┐
              │ UploadRequested     │  → RecordingUploadRequestedEvent
              │  RecordingStatus:   │
              │    Completed        │
              └──────────┬──────────┘
                         │ Upload processing (future: Cloudflare R2)
                         ▼
              ┌─────────────────────┐
              │     Uploaded        │
              │  RecordingStatus:   │
              │    Uploaded         │
              └─────────────────────┘

Failure path:
  Processing/Recording ──► Failed (with RecordingFailureReason)
```

## Aggregate Boundaries

The recording system enforces a strict separation of concerns between two aggregates:

| Aggregate | Owns | Purpose |
|-----------|------|---------|
| `VideoSession` | `Status`, `StartedAt`, `CompletedAt`, `AgoraRecordingId`, `AgoraRecordingSid`, `AgoraRecordingUid`, `RecordingStatus`, `RecordingFailureReason` | Recording lifecycle state — when and how a recording is happening |
| `ScreenRecording` | `Url`, `FileName`, `FileSizeBytes`, `DurationSeconds`, `BucketName`, `ContentType`, `ETag`, `S3UploadedAtUtc`, `StorageProvider`, `AccessControl` | File metadata — what was produced by the recording |

**Key rules:**
- `VideoSession.IsUploadedToS3` delegates to `CurrentRecording?.IsUploadedToS3`
- `VideoSession.ConfirmRecordingStopped` no longer sets file URL — that is the `ScreenRecording`'s responsibility
- `ScreenRecording.SetS3Metadata(bucket, key, contentType, contentLength, etag?)` is the single entry point for recording file metadata
- Agora identifiers (`RecordingId`, `Sid`, `Uid`) exist only on `VideoSession` — they describe the provider session, not the recording output

This prevents data duplication and ensures each aggregate has a single, well-defined reason to change.

## Key Records

| Record | Fields | Source |
|--------|--------|--------|
| `AcquireResult` | `ResourceId` | Agora /acquire |
| `StartRecordingResult` | `ProviderRecordingId`, `ProviderMetadata` (SID) | Agora /start |
| `StopRecordingResult` | `FileUrl`, `DurationSeconds`, `FileSizeBytes`, `Files`, `Summary` | Agora /stop |
| `QueryResult` | `Status` (RecordingStatus), `ResourceId`, `Sid`, `Files`, `Summary` | Agora /query |
| `RecordingFileInfo` | `FileName`, `ObjectKey`, `FileSize`, `StartTime`, `MimeType` | Parsed from Agora fileList |
| `RecordingSummary` | `FileCount`, `TotalSizeBytes` | Computed from fileList |

## Domain Events

| Event | Raised By | Handled By |
|-------|-----------|------------|
| `RecordingStartedEvent` | `VideoSession.SetCurrentRecording()` | `RecordingStartedEventHandler` → SignalR notifier |
| `RecordingCompletedEvent` | `VideoSession.StopRecording()` | `RecordingCompletedEventHandler` → SignalR notifier |
| `RecordingFailedEvent` | `VideoSession.FailActiveRecording()` | `RecordingFailedEventHandler` → SignalR notifier |
| `RecordingUploadRequestedEvent` | `VideoSession.StopRecording()` | `RecordingUploadRequestedEventHandler` → logs + notifier |

## Agora Status Mapping

| Agora Status | RecordingStatus |
|--------------|-----------------|
| `inProgress` / `processing` | `Processing` |
| `stopped` / `completed` | `Completed` |
| `failed` | `Failed` |
| `idle` / `notstarted` | `Idle` |
| `uploading` | `Uploading` |
| `uploaded` | `Uploaded` |
| `starting` | `Starting` |
| `cancelled` / `canceled` | `Cancelled` |

## Configuration (`AgoraSettings`)

| Key | Purpose |
|-----|---------|
| `AppId` | Agora App ID |
| `CustomerId` | Agora RESTful API Customer ID |
| `CustomerSecret` | Agora RESTful API Customer Secret |
| `CloudRecordingBaseUrl` | Base URL for cloud recording API |
| `RecordingMaxIdleTime` | Seconds before auto-stop (default 120) |
| `RecordingStreamTypes` | Stream type (0 = audio+video) |
| `StorageVendor` | Cloud storage vendor (1 = AWS S3) |
| `StorageBucket` | Storage bucket name |
| `StorageAccessKey` / `StorageSecretKey` | Storage credentials |
| `TimeoutSeconds` | HTTP client timeout (default 30) |
| `RetryCount` | Polly retry count for transient errors (default 2) |
| `RecordingMode` | The Agora recording mode to use (`Mix` by default, `Individual`, `Web`) |

## Error Handling

- **Validation errors**: Returned as `Error.Validation()` — client should fix input
- **Authentication errors**: Returned as `Error.Unauthorized()` — check Agora credentials
- **Rate limiting**: Returned as `Error.Failure("Agora.RateLimited")` — Polly retries automatically
- **Server errors**: Returned as `Error.Unexpected("Agora.ServerError")` — Polly retries, then fails
- **Provider call failures in StartRecordingAsync**: Compensation transaction fails the recording
- **Provider call failures in StopRecordingAsync**: Recording NOT marked as completed — state preserved for retry

## Agora Recording Identifiers

The Agora Cloud Recording API utilizes three distinct identifiers, which serve different purposes throughout the recording lifecycle. It is critical that these are not confused or substituted:

1. **`RecordingUid` (Numeric UID)**:
   - A unique numeric identifier generated **before** Acquire.
   - Generated using `GenerateRecordingUid()` (must fit within standard 32-bit unsigned integer range for Agora compatibility).
   - **Important**: Must remain identical across `Acquire`, `Start`, and `Stop`. It is never regenerated or replaced by the SID or ResourceId.
2. **`ResourceId`**:
   - The resource allocation ID returned by the Agora `/acquire` endpoint.
   - Used to reference the allocated cloud recording resource in subsequent API calls.
3. **`SID` (Session ID)**:
   - The session execution ID returned by the Agora `/start` endpoint.
   - Used to reference the active recording instance.

### Identifier Relationship
$$\text{ResourceId} \neq \text{SID} \neq \text{RecordingUid}$$

---

## Identifier Lifecycle flow

The sequence of API calls and state tracking follows this strict lifecycle:

```
Generate UID
     │
     ▼
Build Token(uid) ──► RTC token for the recording client (see below)
     │
     ▼
Acquire(uid) ──► Allocates ResourceId
     │
     ▼
Start(uid, token) ──► Starts session with ResourceId and returns SID
     │
     ▼
Persist(uid) ──► Persists ResourceId, SID, and RecordingUid to DB
     │
     ▼
Stop(uid)    ──► Stops recording using ResourceId, SID, and the persisted RecordingUid
```

## Recording Client Token (secure channels)

The channel is secured with an **App Certificate** (`AgoraSettings.AppCertificate`),
so every client that joins — **including the cloud recording client** — must present a
valid RTC token. The recording client joins with the generated `RecordingUid`, so the
token must be built for that exact uid.

`AgoraRecordingProvider.BuildRecordingToken()` generates it with
`RtcTokenBuilder2.buildTokenWithUid(AppId, AppCertificate, channelName, uid, ...)` —
the same builder used by `AgoraTokenService` for participants — and passes it in the
Start payload as `clientRequest.recordingConfig.token`. When `AppCertificate` is empty
(App ID-only channels) the token is `null` and omitted from the payload.

> **Failure symptom if this is missing:** Acquire / Start / Stop all return success
> (they authenticate with `CustomerId`/`CustomerSecret`, independent of joining the
> channel), but the recording client is silently rejected at channel join, captures no
> media, and **nothing is uploaded to S3** — the stop response has no `fileList` and the
> bucket stays empty. Watch the `"Recording stopped ... fileCount="` log line: `0` means
> the client never joined.
