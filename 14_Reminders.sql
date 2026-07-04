SET NOCOUNT ON;

-- ============================================================
-- Reminders: 1 per confirmed appointment for both client & specialist
-- Some sent in the past (already sent), some in the future
-- ============================================================

WITH ConfirmedAppointments AS (
    SELECT a.Id AS AppointmentId, a.UserId, sp.UserId AS SpecialistUserId, a.Start,
        ROW_NUMBER() OVER (ORDER BY a.Start) AS Seq
    FROM Appointments a
    INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
    WHERE a.Status = N'Confirmed'
)
INSERT INTO Reminders (AppointmentId, UserId, ReminderTime, IsSent, Message, CreatedAtUtc)
SELECT
    ca.AppointmentId,
    ca.UserId,
    DATEADD(HOUR, -2, SYSDATETIME()),
    1,
    N'تذكير بموعد الجلسة مع الأخصائي غداً في الموعد المحدد. يرجى الاستعداد.',
    SYSDATETIMEOFFSET()
FROM ConfirmedAppointments ca
WHERE ca.Seq <= 3  -- Past reminders (already sent)

UNION ALL

SELECT
    ca.AppointmentId,
    ca.SpecialistUserId,
    DATEADD(HOUR, -2, SYSDATETIME()),
    1,
    N'تذكير بموعد الجلسة مع العميل غداً. يرجى تجهيز المواد اللازمة.',
    SYSDATETIMEOFFSET()
FROM ConfirmedAppointments ca
WHERE ca.Seq <= 3

UNION ALL

SELECT
    ca.AppointmentId,
    ca.UserId,
    DATEADD(HOUR, 24, SYSDATETIME()),
    0,
    N'تذكير بموعد الجلسة مع الأخصائي. باقي 24 ساعة على الموعد.',
    SYSDATETIMEOFFSET()
FROM ConfirmedAppointments ca
WHERE ca.Seq > 3  -- Future reminders (not yet sent)

UNION ALL

SELECT
    ca.AppointmentId,
    ca.SpecialistUserId,
    DATEADD(HOUR, 24, SYSDATETIME()),
    0,
    N'تذكير بموعد الجلسة مع العميل. باقي 24 ساعة على الموعد.',
    SYSDATETIMEOFFSET()
FROM ConfirmedAppointments ca
WHERE ca.Seq > 3;

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' reminders.');
GO
