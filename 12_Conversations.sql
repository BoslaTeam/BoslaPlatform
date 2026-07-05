SET NOCOUNT ON;

-- ============================================================
-- Conversations for completed/confirmed appointments (13 total)
-- ============================================================

DECLARE @Conversations TABLE (AppointmentId UNIQUEIDENTIFIER, ConversationId UNIQUEIDENTIFIER);
DECLARE @ConversationParticipants TABLE (ConversationId UNIQUEIDENTIFIER, UserId UNIQUEIDENTIFIER);

-- Create 1 conversation per completed OR confirmed appointment
INSERT INTO Conversations (AppointmentId, CreatedAtUtc)
OUTPUT INSERTED.AppointmentId, INSERTED.Id INTO @Conversations
SELECT a.Id, SYSDATETIMEOFFSET()
FROM Appointments a
WHERE a.Status IN (N'Completed', N'Confirmed');

DECLARE @ConvCount INT; SELECT @ConvCount = COUNT(*) FROM @Conversations;
PRINT CONCAT(N'Inserted ', @ConvCount, N' conversations.');

-- ============================================================
-- ConversationParticipants: 2 per conversation (client + specialist)
-- ============================================================

INSERT INTO ConversationParticipants (ConversationId, UserId, CreatedAtUtc)
SELECT c.ConversationId, a.UserId, SYSDATETIMEOFFSET()
FROM @Conversations c
INNER JOIN Appointments a ON a.Id = c.AppointmentId;

INSERT INTO ConversationParticipants (ConversationId, UserId, CreatedAtUtc)
SELECT c.ConversationId, sp.UserId, SYSDATETIMEOFFSET()
FROM @Conversations c
INNER JOIN Appointments a ON a.Id = c.AppointmentId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId;

DECLARE @ConvParticipantCount INT; SELECT @ConvParticipantCount = 2 * COUNT(*) FROM @Conversations;
PRINT CONCAT(N'Inserted ', @ConvParticipantCount, N' conversation participants.');

-- ============================================================
-- Messages: 3-5 per conversation with realistic Arabic chat
-- ============================================================

-- Message 1: Client greeting / booking confirmation
WITH MsgCte AS (
    SELECT c.ConversationId, a.UserId, ABS(CHECKSUM(NEWID())) % 5 AS Rnd
    FROM @Conversations c
    INNER JOIN Appointments a ON a.Id = c.AppointmentId
)
INSERT INTO Messages (ConversationId, SenderId, MessageText, IsEdited, CreatedAtUtc)
SELECT ConversationId, UserId,
    CASE Rnd
        WHEN 0 THEN N'السلام عليكم، شكراً على قبول الحجز. أنا متحمس للجلسة.'
        WHEN 1 THEN N'مرحباً، تأكدت من موعد الجلسة. إن شاء الله خير.'
        WHEN 2 THEN N'مساء الخير، أنا جاهز للجلسة في الموعد المحدد.'
        WHEN 3 THEN N'أهلاً بك، شكراً لتأكيد الموعد. عندي بعض الأسئلة للجلسة.'
        WHEN 4 THEN N'السلام عليكم، تم الحجز بنجاح. أتطلع للقاء بك.'
    END, 0, SYSDATETIMEOFFSET()
FROM MsgCte;

-- Message 2: Specialist response
WITH MsgCte AS (
    SELECT c.ConversationId, sp.UserId, ABS(CHECKSUM(NEWID())) % 5 AS Rnd
    FROM @Conversations c
    INNER JOIN Appointments a ON a.Id = c.AppointmentId
    INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
)
INSERT INTO Messages (ConversationId, SenderId, MessageText, IsEdited, CreatedAtUtc)
SELECT ConversationId, UserId,
    CASE Rnd
        WHEN 0 THEN N'وعليكم السلام ورحمة الله. أهلاً بك، الجلسة مؤكدة.'
        WHEN 1 THEN N'أهلاً بك، تم تأكيد الحجز. سأكون في انتظارك.'
        WHEN 2 THEN N'مرحباً، موعد الجلسة مؤكد. جهز أسئلتك.'
        WHEN 3 THEN N'أهلاً وسهلاً. أنا سعيد بمساعدتك في الجلسة القادمة.'
        WHEN 4 THEN N'وعليكم السلام. تم تأكيد الحجز، نراكم قريباً.'
    END, 0, SYSDATETIMEOFFSET()
FROM MsgCte;

-- Message 3: Client follow-up question
WITH MsgCte AS (
    SELECT c.ConversationId, a.UserId, ABS(CHECKSUM(NEWID())) % 5 AS Rnd
    FROM @Conversations c
    INNER JOIN Appointments a ON a.Id = c.AppointmentId
)
INSERT INTO Messages (ConversationId, SenderId, MessageText, IsEdited, CreatedAtUtc)
SELECT ConversationId, UserId,
    CASE Rnd
        WHEN 0 THEN N'هل أحتاج لتجهيز أي مستندات قبل الجلسة؟'
        WHEN 1 THEN N'هل الجلسة عبر فيديو أم صوت فقط؟'
        WHEN 2 THEN N'كم مدة الجلسة تقريباً؟'
        WHEN 3 THEN N'هل يمكنني تغيير موعد الجلسة إذا احتجت؟'
        WHEN 4 THEN N'هل ستحتاج مني تحضير أي معلومات مسبقة؟'
    END, 0, SYSDATETIMEOFFSET()
FROM MsgCte;

-- Message 4: Specialist answers
WITH MsgCte AS (
    SELECT c.ConversationId, sp.UserId, ABS(CHECKSUM(NEWID())) % 5 AS Rnd
    FROM @Conversations c
    INNER JOIN Appointments a ON a.Id = c.AppointmentId
    INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
)
INSERT INTO Messages (ConversationId, SenderId, MessageText, IsEdited, CreatedAtUtc)
SELECT ConversationId, UserId,
    CASE Rnd
        WHEN 0 THEN N'نعم، يرجى تجهيز أي استفساراتك المكتوبة.'
        WHEN 1 THEN N'الجلسة عبر الفيديو لمشاركة الشاشة إن احتجنا.'
        WHEN 2 THEN N'مدة الجلسة ساعة كاملة، كافية جداً.'
        WHEN 3 THEN N'يمكنك إعادة جدولة الجلسة قبل 24 ساعة.'
        WHEN 4 THEN N'يكفي أن تحضر أفكارك الرئيسية وسنناقشها معاً.'
    END, 0, SYSDATETIMEOFFSET()
FROM MsgCte;

-- Message 5: Client closing (only for completed appointments)
WITH MsgCte AS (
    SELECT c.ConversationId, a.UserId, ABS(CHECKSUM(NEWID())) % 4 AS Rnd
    FROM @Conversations c
    INNER JOIN Appointments a ON a.Id = c.AppointmentId
    WHERE a.Status = N'Completed'
)
INSERT INTO Messages (ConversationId, SenderId, MessageText, IsEdited, CreatedAtUtc)
SELECT ConversationId, UserId,
    CASE Rnd
        WHEN 0 THEN N'شكراً جزيلاً على الجلسة المفيدة جداً.'
        WHEN 1 THEN N'استفدت كثيراً من الجلسة، شكراً لوقتك.'
        WHEN 2 THEN N'كانت جلسة ممتازة، سأطبق النصائح التي أعطيتني إياها.'
        WHEN 3 THEN N'شكراً لك، كانت المعلومات قيّمة جداً وسأحجز جلسة أخرى.'
    END, 0, SYSDATETIMEOFFSET()
FROM MsgCte;

PRINT N'Inserted messages for all conversations.';
GO
