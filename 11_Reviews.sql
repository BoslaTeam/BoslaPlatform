SET NOCOUNT ON;

-- ============================================================
-- Reviews for all 8 Completed appointments
-- Each review by the client who booked, about the specialist
-- ============================================================

WITH CompletedAppointments AS (
    SELECT TOP 8 a.Id AS AppointmentId, a.UserId, a.SpecialistId,
        ROW_NUMBER() OVER (ORDER BY a.Start) AS Seq
    FROM Appointments a
    WHERE a.Status = N'Completed'
    ORDER BY a.Start
)
INSERT INTO Reviews (AppointmentId, ReviewerId, SpecialistId, Rating, Comment, CreatedAtUtc)
SELECT
    ca.AppointmentId,
    ca.UserId,
    ca.SpecialistId,
    CASE ca.Seq
        WHEN 1 THEN 5
        WHEN 2 THEN 4
        WHEN 3 THEN 5
        WHEN 4 THEN 3
        WHEN 5 THEN 5
        WHEN 6 THEN 4
        WHEN 7 THEN 2
        WHEN 8 THEN 4
    END,
    CASE ca.Seq
        WHEN 1 THEN N'جلسة ممتازة، استفدت كثيراً من خبرة الدكتور. أنصح بالتعامل معه.'
        WHEN 2 THEN N'استشارة مفيدة جداً، شكراً لوقتك وجهدك.'
        WHEN 3 THEN N'خدمة رائعة ومهنية عالية. سأحجز جلسة أخرى قريباً.'
        WHEN 4 THEN N'الجلسة كانت متوسطة، كان يمكن أن تكون أفضل في التنظيم.'
        WHEN 5 THEN N'خبير متمكن جداً، شرح كل شيء بوضوح. تجربة ممتازة.'
        WHEN 6 THEN N'التزام بالموعد ومعلومات قيمة. شكراً جزيلاً.'
        WHEN 7 THEN N'لم أستفد كثيراً من الجلسة، التوقعات كانت أعلى.'
        WHEN 8 THEN N'جيد جداً، سأكرر التجربة.'
    END,
    SYSDATETIMEOFFSET()
FROM CompletedAppointments ca;

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' reviews.');
GO
