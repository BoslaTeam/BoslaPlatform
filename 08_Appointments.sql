-- ============================================================
-- 08_Appointments.sql
-- 25 Appointments + Status History for Bosla Platform Demo
-- Depends on: 01_Users.sql (client users), 02_Specialists.sql
-- ============================================================

SET NOCOUNT ON;

-- ============================================================
-- 1. Insert 25 Appointments
-- Statuses: 5 Pending, 5 Confirmed, 8 Completed, 4 Cancelled, 3 Rescheduled
-- ============================================================
INSERT INTO Appointments (SpecialistId, UserId, Start, [End], Status, SessionTopic, Notes, CancellationReason, SessionPrice, CreatedAtUtc)
SELECT sp.Id, u.Id, D.StartDt, D.EndDt, D.Status, D.Topic, D.Notes, D.CancelReason, sp.HourlyRate, SYSDATETIMEOFFSET()
FROM (VALUES
    -- ======== 5 Pending (next few days) ========
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-07-06T10:00:00+02:00', '2026-07-06T11:00:00+02:00', N'Pending',   N'طلب استشارة في تطوير تطبيقات الويب',        NULL, NULL),
    (N'eman.hassan@bosla.demo',   N'sara.mohamed@bosla.demo',  '2026-07-07T09:00:00+02:00', '2026-07-07T10:00:00+02:00', N'Pending',   N'استشارة في تصميم واجهات المستخدم',          NULL, NULL),
    (N'ahmed.adel@bosla.demo',    N'mostafa.kamal@bosla.demo', '2026-07-08T08:00:00+02:00', '2026-07-08T09:00:00+02:00', N'Pending',   N'استشارة في البنية التحتية للتطبيق',          NULL, NULL),
    (N'samia.nabil@bosla.demo',   N'tarek.mahmoud@bosla.demo', '2026-07-09T08:00:00+02:00', '2026-07-09T09:00:00+02:00', N'Pending',   N'استشارة في الحلول السحابية',                NULL, NULL),
    (N'khaled.mohsen@bosla.demo', N'omar.nabil@bosla.demo',    '2026-07-06T10:00:00+02:00', '2026-07-06T11:00:00+02:00', N'Pending',   N'استشارة طبية عامة',                         NULL, NULL),

    -- ======== 5 Confirmed (upcoming, next 1-3 days) ========
    (N'noha.ashraf@bosla.demo',   N'ali.abdelrahman@bosla.demo',  '2026-07-06T08:00:00+02:00', '2026-07-06T09:00:00+02:00', N'Confirmed', N'جلسة مراجعة واجهات المستخدم',              NULL, NULL),
    (N'yasser.ibrahim@bosla.demo', N'rania.abdelaziz@bosla.demo', '2026-07-06T10:00:00+02:00', '2026-07-06T11:00:00+02:00', N'Confirmed', N'مراجعة استراتيجية DevOps',                  NULL, NULL),
    (N'laila.ahmed@bosla.demo',   N'nermeen.adel@bosla.demo',     '2026-07-06T10:00:00+02:00', '2026-07-06T11:00:00+02:00', N'Confirmed', N'جلسة تحليل بيانات المشروع',                NULL, NULL),
    (N'hesham.tawfik@bosla.demo', N'khaled.ali@bosla.demo',       '2026-07-07T09:00:00+02:00', '2026-07-07T10:00:00+02:00', N'Confirmed', N'استشارة في زراعة الأسنان',                  NULL, NULL),
    (N'rana.adel@bosla.demo',     N'marwa.said@bosla.demo',       '2026-07-08T11:00:00+02:00', '2026-07-08T12:00:00+02:00', N'Confirmed', N'جلسة استراتيجية تسويقية',                   NULL, NULL),

    -- ======== 8 Completed (past, 1-4 weeks ago) ========
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-06-08T10:00:00+02:00', '2026-06-08T11:00:00+02:00', N'Completed', N'استشارة في تطوير تطبيقات الويب',        N'تمت الجلسة بنجاح وتم الاتفاق على خارطة الطريق.', NULL),
    (N'eman.hassan@bosla.demo',   N'mona.ibrahim@bosla.demo',  '2026-06-10T10:00:00+02:00', '2026-06-10T11:00:00+02:00', N'Completed', N'تطوير APIs للمشروع',                     N'تمت مراجعة تصميم API وتقديم التوصيات.', NULL),
    (N'ahmed.adel@bosla.demo',    N'sara.mohamed@bosla.demo',  '2026-06-15T09:00:00+02:00', '2026-06-15T10:00:00+02:00', N'Completed', N'تصميم واجهة المستخدم للتطبيق',           N'تم تقديم تصميم أولي وملاحظات.', NULL),
    (N'samia.nabil@bosla.demo',   N'dina.essam@bosla.demo',    '2026-06-17T10:00:00+02:00', '2026-06-17T11:00:00+02:00', N'Completed', N'تطوير تطبيق جوال',                      N'مناقشة متطلبات التطبيق واختيار التقنيات المناسبة.', NULL),
    (N'khaled.mohsen@bosla.demo', N'tarek.mahmoud@bosla.demo', '2026-06-18T08:00:00+02:00', '2026-06-18T09:00:00+02:00', N'Completed', N'تخطيط استراتيجي للمشروع',                N'تم وضع خطة شاملة للبنية التحتية السحابية.', NULL),
    (N'noha.ashraf@bosla.demo',   N'ashraf.khalil@bosla.demo', '2026-06-22T09:00:00+02:00', '2026-06-22T10:00:00+02:00', N'Completed', N'تحليل البيانات والتقارير',               N'تقديم حلول تحليل البيانات وتصورها.', NULL),
    (N'yasser.ibrahim@bosla.demo', N'fatma.elzahraa@bosla.demo', '2026-06-24T10:00:00+02:00', '2026-06-24T11:00:00+02:00', N'Completed', N'تدريب على التعلم الآلي',                 N'جلسة تدريبية مكثفة في نماذج التعلم الآلي.', NULL),
    (N'laila.ahmed@bosla.demo',   N'laila.mostafa@bosla.demo', '2026-06-29T10:00:00+02:00', '2026-06-29T11:00:00+02:00', N'Completed', N'استشارة في التحول الرقمي',                N'تقييم الوضع الحالي ووضع خطة التحول الرقمي.', NULL),

    -- ======== 4 Cancelled ========
    (N'mohamed.reda@bosla.demo',  N'ali.abdelrahman@bosla.demo',  '2026-06-03T08:00:00+02:00', '2026-06-03T09:00:00+02:00', N'Cancelled', N'مراجعة الكود',                 NULL, N'تعارض في المواعيد'),
    (N'eman.hassan@bosla.demo',   N'mostafa.kamal@bosla.demo',    '2026-06-10T08:00:00+02:00', '2026-06-10T09:00:00+02:00', N'Cancelled', N'استشارة DevOps',               NULL, N'تم إلغاء المشروع'),
    (N'ahmed.adel@bosla.demo',    N'khaled.ali@bosla.demo',       '2026-06-17T09:00:00+02:00', '2026-06-17T10:00:00+02:00', N'Cancelled', N'استشارة أسنان',                NULL, N'ظرف طارئ للعميل'),
    (N'samia.nabil@bosla.demo',   N'hany.magdy@bosla.demo',       '2026-06-24T08:00:00+02:00', '2026-06-24T09:00:00+02:00', N'Cancelled', N'استشارة إدارية',               NULL, N'تغيير أولويات الشركة'),

    -- ======== 3 Rescheduled ========
    (N'khaled.mohsen@bosla.demo', N'mona.ibrahim@bosla.demo',  '2026-07-01T10:00:00+02:00', '2026-07-01T11:00:00+02:00', N'Rescheduled', N'استشارة برمجيات',        N'تم إعادة جدولة الجلسة من 28 يونيو إلى 1 يوليو.', NULL),
    (N'noha.ashraf@bosla.demo',   N'nermeen.adel@bosla.demo',  '2026-07-01T10:00:00+02:00', '2026-07-01T11:00:00+02:00', N'Rescheduled', N'تحليل بيانات',            N'تم إعادة الجدولة من 29 يونيو بسبب ظروف العميل.', NULL),
    (N'yasser.ibrahim@bosla.demo', N'amr.elsayed@bosla.demo',  '2026-06-30T08:00:00+02:00', '2026-06-30T09:00:00+02:00', N'Rescheduled', N'استشارة هندسية',          N'إعادة جدولة بسبب تعارض المواعيد.', NULL)
) AS D(ClientEmail, SpecialistEmail, StartDt, EndDt, Status, Topic, Notes, CancelReason)
INNER JOIN AspNetUsers u ON u.Email = D.ClientEmail
INNER JOIN AspNetUsers su ON su.Email = D.SpecialistEmail
INNER JOIN Specialists sp ON sp.UserId = su.Id
WHERE NOT EXISTS (SELECT 1 FROM Appointments a WHERE a.SpecialistId = sp.Id AND a.UserId = u.Id AND a.Start = D.StartDt);

DECLARE @ApptCount INT = @@ROWCOUNT;
PRINT CONCAT(N'Inserted ', @ApptCount, N' appointments.');

-- ============================================================
-- 2. Insert AppointmentStatusHistory
-- ============================================================

-- 2a. Initial Pending -> Pending for all 25 appointments
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Pending', N'Pending', N'Initial booking request.', SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-07-06T10:00:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'sara.mohamed@bosla.demo',  '2026-07-07T09:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'mostafa.kamal@bosla.demo', '2026-07-08T08:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'tarek.mahmoud@bosla.demo', '2026-07-09T08:00:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'omar.nabil@bosla.demo',    '2026-07-06T10:00:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'ali.abdelrahman@bosla.demo',  '2026-07-06T08:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'rania.abdelaziz@bosla.demo', '2026-07-06T10:00:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'nermeen.adel@bosla.demo',     '2026-07-06T10:00:00+02:00'),
    (N'hesham.tawfik@bosla.demo', N'khaled.ali@bosla.demo',       '2026-07-07T09:00:00+02:00'),
    (N'rana.adel@bosla.demo',     N'marwa.said@bosla.demo',       '2026-07-08T11:00:00+02:00'),
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-06-08T10:00:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'mona.ibrahim@bosla.demo',  '2026-06-10T10:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'sara.mohamed@bosla.demo',  '2026-06-15T09:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'dina.essam@bosla.demo',    '2026-06-17T10:00:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'tarek.mahmoud@bosla.demo', '2026-06-18T08:00:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'ashraf.khalil@bosla.demo', '2026-06-22T09:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'fatma.elzahraa@bosla.demo', '2026-06-24T10:00:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'laila.mostafa@bosla.demo', '2026-06-29T10:00:00+02:00'),
    (N'mohamed.reda@bosla.demo',  N'ali.abdelrahman@bosla.demo',  '2026-06-03T08:00:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'mostafa.kamal@bosla.demo',    '2026-06-10T08:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'khaled.ali@bosla.demo',       '2026-06-17T09:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'hany.magdy@bosla.demo',       '2026-06-24T08:00:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'mona.ibrahim@bosla.demo',  '2026-07-01T10:00:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'nermeen.adel@bosla.demo',  '2026-07-01T10:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'amr.elsayed@bosla.demo',  '2026-06-30T08:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.NewStatus = N'Pending');

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' initial Pending status history records.');

-- 2b. Pending -> Confirmed for Confirmed and Completed appointments
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Pending', N'Confirmed', N'Appointment confirmed by specialist.', SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    -- Confirmed (5)
    (N'noha.ashraf@bosla.demo',   N'ali.abdelrahman@bosla.demo',  '2026-07-06T08:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'rania.abdelaziz@bosla.demo', '2026-07-06T10:00:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'nermeen.adel@bosla.demo',     '2026-07-06T10:00:00+02:00'),
    (N'hesham.tawfik@bosla.demo', N'khaled.ali@bosla.demo',       '2026-07-07T09:00:00+02:00'),
    (N'rana.adel@bosla.demo',     N'marwa.said@bosla.demo',       '2026-07-08T11:00:00+02:00'),
    -- Completed (8)
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-06-08T10:00:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'mona.ibrahim@bosla.demo',  '2026-06-10T10:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'sara.mohamed@bosla.demo',  '2026-06-15T09:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'dina.essam@bosla.demo',    '2026-06-17T10:00:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'tarek.mahmoud@bosla.demo', '2026-06-18T08:00:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'ashraf.khalil@bosla.demo', '2026-06-22T09:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'fatma.elzahraa@bosla.demo', '2026-06-24T10:00:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'laila.mostafa@bosla.demo', '2026-06-29T10:00:00+02:00'),
    -- Cancelled that were confirmed first: eman.hassan→mostafa.kamal, samia.nabil→hany.magdy
    (N'eman.hassan@bosla.demo',   N'mostafa.kamal@bosla.demo',    '2026-06-10T08:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'hany.magdy@bosla.demo',       '2026-06-24T08:00:00+02:00'),
    -- Rescheduled that were confirmed first: noha.ashraf→nermeen.adel
    (N'noha.ashraf@bosla.demo',   N'nermeen.adel@bosla.demo',  '2026-07-01T10:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.NewStatus = N'Confirmed');

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' Confirmed status transitions.');

-- 2c. Confirmed -> Completed for 8 Completed appointments
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Confirmed', N'Completed', N'Session completed successfully.', SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-06-08T10:00:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'mona.ibrahim@bosla.demo',  '2026-06-10T10:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'sara.mohamed@bosla.demo',  '2026-06-15T09:00:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'dina.essam@bosla.demo',    '2026-06-17T10:00:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'tarek.mahmoud@bosla.demo', '2026-06-18T08:00:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'ashraf.khalil@bosla.demo', '2026-06-22T09:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'fatma.elzahraa@bosla.demo', '2026-06-24T10:00:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'laila.mostafa@bosla.demo', '2026-06-29T10:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.NewStatus = N'Completed');

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' Completed status transitions.');

-- 2d. Pending -> Cancelled (cancelled before confirmation)
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Pending', N'Cancelled', N'Appointment cancelled before confirmation: ' + a.CancellationReason, SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'mohamed.reda@bosla.demo', N'ali.abdelrahman@bosla.demo', '2026-06-03T08:00:00+02:00'),
    (N'ahmed.adel@bosla.demo',   N'khaled.ali@bosla.demo',      '2026-06-17T09:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.NewStatus = N'Cancelled');

-- 2e. Confirmed -> Cancelled (cancelled after confirmation)
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Confirmed', N'Cancelled', N'Appointment cancelled after confirmation: ' + a.CancellationReason, SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'eman.hassan@bosla.demo', N'mostafa.kamal@bosla.demo', '2026-06-10T08:00:00+02:00'),
    (N'samia.nabil@bosla.demo', N'hany.magdy@bosla.demo',    '2026-06-24T08:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.OldStatus = N'Confirmed' AND h.NewStatus = N'Cancelled');

PRINT CONCAT(N'Inserted cancellation status transitions (Pending/Confirmed ', N'-> Cancelled).');

-- 2f. Pending -> Rescheduled (rescheduled before confirmation)
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Pending', N'Rescheduled', N'Rescheduled: requested by client before confirmation.', SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'khaled.mohsen@bosla.demo', N'mona.ibrahim@bosla.demo', '2026-07-01T10:00:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'amr.elsayed@bosla.demo', '2026-06-30T08:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.NewStatus = N'Rescheduled');

-- 2g. Confirmed -> Rescheduled (rescheduled after confirmation)
INSERT INTO AppointmentStatusHistory (AppointmentId, OldStatus, NewStatus, Reason, CreatedAtUtc)
SELECT a.Id, N'Confirmed', N'Rescheduled', N'Rescheduled: client requested new date after confirmation.', SYSDATETIMEOFFSET()
FROM Appointments a
INNER JOIN AspNetUsers u ON u.Id = a.UserId
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId
INNER JOIN (VALUES
    (N'noha.ashraf@bosla.demo', N'nermeen.adel@bosla.demo', '2026-07-01T10:00:00+02:00')
) AS D(ClientEmail, SpecialistEmail, StartDt)
    ON u.Email = D.ClientEmail AND su.Email = D.SpecialistEmail AND a.Start = D.StartDt
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatusHistory h WHERE h.AppointmentId = a.Id AND h.OldStatus = N'Confirmed' AND h.NewStatus = N'Rescheduled');

PRINT CONCAT(N'Inserted reschedule status transitions (Pending/Confirmed ', N'-> Rescheduled).');

PRINT N'Appointments and status history seeded successfully.';
GO
