SET NOCOUNT ON;

-- ============================================================
-- 10_VideoSessions.sql
-- VideoSessions, VideoSessionParticipants, ScreenRecordings
-- ============================================================

DECLARE @CompletedSessions TABLE (AppointmentId UNIQUEIDENTIFIER, VideoSessionId UNIQUEIDENTIFIER);
DECLARE @WaitingSessions TABLE (AppointmentId UNIQUEIDENTIFIER, VideoSessionId UNIQUEIDENTIFIER);
DECLARE @RescheduledSessions TABLE (AppointmentId UNIQUEIDENTIFIER, VideoSessionId UNIQUEIDENTIFIER);

-- ============================================================
-- 1) 8 Completed appointments → Status 'Completed'
--    StartedAt 1-4 weeks ago, EndedAt 1 hour later
-- ============================================================
WITH Numbered AS (
    SELECT a.Id, a.SpecialistId,
        ROW_NUMBER() OVER (ORDER BY a.Start) AS Seq
    FROM Appointments a
    WHERE a.Status = N'Completed'
)
INSERT INTO VideoSessions (AppointmentId, Type, AgoraChannelName, AgoraAppId, Status, StartedAt, EndedAt, CreatedAtUtc)
OUTPUT INSERTED.AppointmentId, INSERTED.Id INTO @CompletedSessions
SELECT TOP 8
    n.Id,
    CASE WHEN EXISTS (
        SELECT 1 FROM SpecialistExpertise se
        INNER JOIN Expertises e ON e.Id = se.ExpertiseId
        WHERE se.SpecialistId = n.SpecialistId AND e.Name IN (N'قانون', N'محاسبة')
    ) THEN N'AudioCall' ELSE N'VideoCall' END,
    N'session_' + CAST(NEWID() AS NVARCHAR(36)),
    N'agora_app_demo_id',
    N'Completed',
    DATEADD(DAY, -28 + (n.Seq - 1) * 3, SYSDATETIME()),
    DATEADD(HOUR, 1, DATEADD(DAY, -28 + (n.Seq - 1) * 3, SYSDATETIME())),
    SYSDATETIMEOFFSET()
FROM Numbered n;

DECLARE @CompletedCount INT; SELECT @CompletedCount = COUNT(*) FROM @CompletedSessions;
PRINT CONCAT(N'Inserted ', @CompletedCount, N' video sessions for Completed appointments.');

-- ============================================================
-- 2) 5 Confirmed appointments → Status 'Waiting'
-- ============================================================
WITH Numbered AS (
    SELECT a.Id, a.SpecialistId,
        ROW_NUMBER() OVER (ORDER BY a.Start) AS Seq
    FROM Appointments a
    WHERE a.Status = N'Confirmed'
)
INSERT INTO VideoSessions (AppointmentId, Type, AgoraChannelName, AgoraAppId, Status, StartedAt, EndedAt, CreatedAtUtc)
OUTPUT INSERTED.AppointmentId, INSERTED.Id INTO @WaitingSessions
SELECT TOP 5
    n.Id,
    CASE WHEN EXISTS (
        SELECT 1 FROM SpecialistExpertise se
        INNER JOIN Expertises e ON e.Id = se.ExpertiseId
        WHERE se.SpecialistId = n.SpecialistId AND e.Name IN (N'قانون', N'محاسبة')
    ) THEN N'AudioCall' ELSE N'VideoCall' END,
    N'session_' + CAST(NEWID() AS NVARCHAR(36)),
    N'agora_app_demo_id',
    N'Waiting',
    NULL,
    NULL,
    SYSDATETIMEOFFSET()
FROM Numbered n;

DECLARE @WaitingCount INT; SELECT @WaitingCount = COUNT(*) FROM @WaitingSessions;
PRINT CONCAT(N'Inserted ', @WaitingCount, N' video sessions for Confirmed appointments.');

-- ============================================================
-- 3) 3 Rescheduled appointments → Status 'Completed' (old sessions already happened)
-- ============================================================
WITH Numbered AS (
    SELECT a.Id, a.SpecialistId,
        ROW_NUMBER() OVER (ORDER BY a.Start) AS Seq
    FROM Appointments a
    WHERE a.Status = N'Rescheduled'
)
INSERT INTO VideoSessions (AppointmentId, Type, AgoraChannelName, AgoraAppId, Status, StartedAt, EndedAt, CreatedAtUtc)
OUTPUT INSERTED.AppointmentId, INSERTED.Id INTO @RescheduledSessions
SELECT TOP 3
    n.Id,
    CASE WHEN EXISTS (
        SELECT 1 FROM SpecialistExpertise se
        INNER JOIN Expertises e ON e.Id = se.ExpertiseId
        WHERE se.SpecialistId = n.SpecialistId AND e.Name IN (N'قانون', N'محاسبة')
    ) THEN N'AudioCall' ELSE N'VideoCall' END,
    N'session_' + CAST(NEWID() AS NVARCHAR(36)),
    N'agora_app_demo_id',
    N'Completed',
    DATEADD(DAY, -21 + (n.Seq - 1) * 7, SYSDATETIME()),
    DATEADD(HOUR, 1, DATEADD(DAY, -21 + (n.Seq - 1) * 7, SYSDATETIME())),
    SYSDATETIMEOFFSET()
FROM Numbered n;

DECLARE @RescheduledCount INT; SELECT @RescheduledCount = COUNT(*) FROM @RescheduledSessions;
PRINT CONCAT(N'Inserted ', @RescheduledCount, N' video sessions for Rescheduled appointments.');

-- ============================================================
-- VideoSessionParticipants: 2 per session (specialist + client)
-- ============================================================

-- Completed sessions (8 + 3 = 11): include JoinedAt/LeftAt timestamps
INSERT INTO VideoSessionParticipants (VideoSessionId, UserId, AgoraUid, Role, JoinedAt, LeftAt, CreatedAtUtc)
SELECT
    cs.VideoSessionId,
    a.UserId,
    2000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Client',
    DATEADD(MINUTE, -2, vs.StartedAt),
    vs.EndedAt,
    SYSDATETIMEOFFSET()
FROM @CompletedSessions cs
INNER JOIN VideoSessions vs ON vs.Id = cs.VideoSessionId
INNER JOIN Appointments a ON a.Id = cs.AppointmentId
UNION ALL
SELECT
    cs.VideoSessionId,
    sp.UserId,
    1000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Specialist',
    vs.StartedAt,
    vs.EndedAt,
    SYSDATETIMEOFFSET()
FROM @CompletedSessions cs
INNER JOIN VideoSessions vs ON vs.Id = cs.VideoSessionId
INNER JOIN Appointments a ON a.Id = cs.AppointmentId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
UNION ALL
SELECT
    rs.VideoSessionId,
    a.UserId,
    2000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Client',
    DATEADD(MINUTE, -2, vs.StartedAt),
    vs.EndedAt,
    SYSDATETIMEOFFSET()
FROM @RescheduledSessions rs
INNER JOIN VideoSessions vs ON vs.Id = rs.VideoSessionId
INNER JOIN Appointments a ON a.Id = rs.AppointmentId
UNION ALL
SELECT
    rs.VideoSessionId,
    sp.UserId,
    1000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Specialist',
    vs.StartedAt,
    vs.EndedAt,
    SYSDATETIMEOFFSET()
FROM @RescheduledSessions rs
INNER JOIN VideoSessions vs ON vs.Id = rs.VideoSessionId
INNER JOIN Appointments a ON a.Id = rs.AppointmentId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId;

-- Waiting sessions: no timestamps
INSERT INTO VideoSessionParticipants (VideoSessionId, UserId, AgoraUid, Role, JoinedAt, LeftAt, CreatedAtUtc)
SELECT
    ws.VideoSessionId,
    a.UserId,
    2000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Client',
    NULL,
    NULL,
    SYSDATETIMEOFFSET()
FROM @WaitingSessions ws
INNER JOIN Appointments a ON a.Id = ws.AppointmentId
UNION ALL
SELECT
    ws.VideoSessionId,
    sp.UserId,
    1000000 + ABS(CHECKSUM(NEWID())) % 9000000,
    N'Specialist',
    NULL,
    NULL,
    SYSDATETIMEOFFSET()
FROM @WaitingSessions ws
INNER JOIN Appointments a ON a.Id = ws.AppointmentId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId;

DECLARE @TotalParticipants INT;
SELECT @TotalParticipants = 4 * (SELECT COUNT(*) FROM @CompletedSessions)
                         + 4 * (SELECT COUNT(*) FROM @RescheduledSessions)
                         + 2 * (SELECT COUNT(*) FROM @WaitingSessions);
PRINT CONCAT(N'Inserted ', @TotalParticipants, N' video session participants.');

-- ============================================================
-- ScreenRecordings: 1 per completed video session + 2 pending
-- ============================================================

DECLARE @RecordingUrlBase NVARCHAR(100) = N'https://agora-recordings.bosla.demo/';

-- Completed recordings for completed sessions (8 from Completed + 3 from Rescheduled = 11)
INSERT INTO ScreenRecordings (VideoSessionId, Url, Status, DurationSeconds, StorageProvider, AccessControl, AgoraRecordingId, AgoraRecordingSid, CreatedAtUtc)
SELECT
    cs.VideoSessionId,
    @RecordingUrlBase + CAST(cs.VideoSessionId AS NVARCHAR(36)) + N'_recording.mp4',
    N'Completed',
    1800 + ABS(CHECKSUM(NEWID())) % 2700,
    N'Agora',
    N'SpecialistOnly',
    N'rec_' + CAST(NEWID() AS NVARCHAR(36)),
    N'sid_' + CAST(NEWID() AS NVARCHAR(36)),
    SYSDATETIMEOFFSET()
FROM @CompletedSessions cs
UNION ALL
SELECT
    rs.VideoSessionId,
    @RecordingUrlBase + CAST(rs.VideoSessionId AS NVARCHAR(36)) + N'_recording.mp4',
    N'Completed',
    1800 + ABS(CHECKSUM(NEWID())) % 2700,
    N'Agora',
    N'SpecialistOnly',
    N'rec_' + CAST(NEWID() AS NVARCHAR(36)),
    N'sid_' + CAST(NEWID() AS NVARCHAR(36)),
    SYSDATETIMEOFFSET()
FROM @RescheduledSessions rs;

-- 2 pending recordings from waiting sessions
WITH PendingVideoSessions AS (
    SELECT TOP 2 ws.VideoSessionId
    FROM @WaitingSessions ws
)
INSERT INTO ScreenRecordings (VideoSessionId, Url, Status, DurationSeconds, StorageProvider, AccessControl, AgoraRecordingId, AgoraRecordingSid, CreatedAtUtc)
SELECT
    pvs.VideoSessionId,
    @RecordingUrlBase + CAST(pvs.VideoSessionId AS NVARCHAR(36)) + N'_pending_recording.mp4',
    N'Pending',
    NULL,
    N'Agora',
    N'SpecialistOnly',
    NULL,
    NULL,
    SYSDATETIMEOFFSET()
FROM PendingVideoSessions pvs;

PRINT N'Inserted screen recordings.';
GO
