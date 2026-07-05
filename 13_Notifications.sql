SET NOCOUNT ON;

-- ============================================================
-- Notifications: 2-3 per appointment (~32-38 total)
-- For Completed, Confirmed, and Rescheduled appointments
-- ============================================================

-- Notification 1: Booking confirmation for the client
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, AppointmentId, CreatedAtUtc)
SELECT a.UserId,
    N'تأكيد الحجز',
    CASE a.Status
        WHEN N'Completed' THEN N'تم تأكيد حجزك مع الأخصائي. الجلسة اكتملت بنجاح.'
        WHEN N'Confirmed' THEN N'تم تأكيد موعد الحجز. يرجى الاستعداد للجلسة.'
        WHEN N'Rescheduled' THEN N'تم إعادة جدولة موعد الجلسة بنجاح.'
    END,
    N'Booking',
    CASE WHEN a.Status = N'Completed' THEN 1 ELSE 0 END,
    a.Id,
    SYSDATETIMEOFFSET()
FROM Appointments a
WHERE a.Status IN (N'Completed', N'Confirmed', N'Rescheduled');

-- Notification 2: Update for the specialist
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, AppointmentId, CreatedAtUtc)
SELECT sp.UserId,
    CASE a.Status
        WHEN N'Completed' THEN N'اكتملت الجلسة'
        WHEN N'Confirmed' THEN N'حجز جديد'
        WHEN N'Rescheduled' THEN N'إعادة جدولة'
    END,
    CASE a.Status
        WHEN N'Completed' THEN N'اكتملت الجلسة مع العميل. يمكنك مراجعة ملخص الجلسة.'
        WHEN N'Confirmed' THEN N'لديك حجز جديد مؤكد. يرجى التحضير للجلسة.'
        WHEN N'Rescheduled' THEN N'تم إعادة جدولة موعد الجلسة من قبل العميل.'
    END,
    N'Booking',
    CASE WHEN a.Status = N'Completed' THEN 1 ELSE 0 END,
    a.Id,
    SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
WHERE a.Status IN (N'Completed', N'Confirmed', N'Rescheduled');

-- Notification 3: Reminder-style notification (subset of confirmed)
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, AppointmentId, CreatedAtUtc)
SELECT
    CASE WHEN n.IsClient = 1 THEN a.UserId ELSE sp.UserId END,
    N'تذكير بالموعد',
    CASE
        WHEN n.IsClient = 1 THEN N'موعد جلستك مع الأخصائي غداً. يرجى الاستعداد.'
        ELSE N'لديك جلسة مع العميل غداً. يرجى تجهيز المواد.'
    END,
    N'Reminder',
    0,
    a.Id,
    SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
CROSS JOIN (VALUES (1), (0)) AS n(IsClient)
WHERE a.Status = N'Confirmed'
AND ABS(CHECKSUM(NEWID())) % 2 = 0;

PRINT N'Inserted notifications.';
GO
