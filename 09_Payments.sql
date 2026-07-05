-- ============================================================
-- 09_Payments.sql
-- Payments for Appointments - Bosla Platform Demo
-- Depends on: 08_Appointments.sql
-- ============================================================
-- Payment calculation (matches Payment.Initiate factory):
--   TaxAmount        = ROUND(SessionPrice * 0.05, 2)
--   PlatformFeeAmount = ROUND(SessionPrice * 0.10, 2)
--   SpecialistAmount  = SessionPrice - PlatformFeeAmount
--   Amount            = SessionPrice + TaxAmount
-- ============================================================

SET NOCOUNT ON;

INSERT INTO Payments (AppointmentId, Amount, Currency, Status, PaymentMethod, ExternalPaymentId, PaidAt, PlatformFeeAmount, SpecialistAmount, TaxAmount, CreatedAtUtc)
SELECT a.Id,
    a.SessionPrice + ROUND(a.SessionPrice * 0.05, 2),
    N'USD',
    D.PayStatus,
    D.Method,
    D.ExtId,
    D.PaidAt,
    ROUND(a.SessionPrice * 0.10, 2),
    a.SessionPrice - ROUND(a.SessionPrice * 0.10, 2),
    ROUND(a.SessionPrice * 0.05, 2),
    SYSDATETIMEOFFSET()
FROM (VALUES
    -- ======== 8 Completed appointments -> Payment Completed, PaidAt in past ========
    (N'mohamed.reda@bosla.demo',  N'ahmed.hassan@bosla.demo',  '2026-06-08T10:00:00+02:00', N'Completed', N'بطاقة ائتمان',      N'bosla_pay_001', '2026-06-08T11:05:00+02:00'),
    (N'eman.hassan@bosla.demo',   N'mona.ibrahim@bosla.demo',  '2026-06-10T10:00:00+02:00', N'Completed', N'محفظة إلكترونية',   N'bosla_pay_002', '2026-06-10T11:05:00+02:00'),
    (N'ahmed.adel@bosla.demo',    N'sara.mohamed@bosla.demo',  '2026-06-15T09:00:00+02:00', N'Completed', N'تحويل بنكي',        N'bosla_pay_003', '2026-06-15T10:05:00+02:00'),
    (N'samia.nabil@bosla.demo',   N'dina.essam@bosla.demo',    '2026-06-17T10:00:00+02:00', N'Completed', N'بطاقة ائتمان',      N'bosla_pay_004', '2026-06-17T11:05:00+02:00'),
    (N'khaled.mohsen@bosla.demo', N'tarek.mahmoud@bosla.demo', '2026-06-18T08:00:00+02:00', N'Completed', N'محفظة إلكترونية',   N'bosla_pay_005', '2026-06-18T09:05:00+02:00'),
    (N'noha.ashraf@bosla.demo',   N'ashraf.khalil@bosla.demo', '2026-06-22T09:00:00+02:00', N'Completed', N'تحويل بنكي',        N'bosla_pay_006', '2026-06-22T10:05:00+02:00'),
    (N'yasser.ibrahim@bosla.demo', N'fatma.elzahraa@bosla.demo', '2026-06-24T10:00:00+02:00', N'Completed', N'بطاقة ائتمان',    N'bosla_pay_007', '2026-06-24T11:05:00+02:00'),
    (N'laila.ahmed@bosla.demo',   N'laila.mostafa@bosla.demo', '2026-06-29T10:00:00+02:00', N'Completed', N'محفظة إلكترونية',   N'bosla_pay_008', '2026-06-29T11:05:00+02:00'),

    -- ======== 5 Confirmed appointments -> Payment Pending, PaidAt NULL (paid but session upcoming) ========
    (N'noha.ashraf@bosla.demo',   N'ali.abdelrahman@bosla.demo',  '2026-07-06T08:00:00+02:00', N'Pending', N'بطاقة ائتمان',    NULL, NULL),
    (N'yasser.ibrahim@bosla.demo', N'rania.abdelaziz@bosla.demo', '2026-07-06T10:00:00+02:00', N'Pending', N'محفظة إلكترونية', NULL, NULL),
    (N'laila.ahmed@bosla.demo',   N'nermeen.adel@bosla.demo',     '2026-07-06T10:00:00+02:00', N'Pending', N'تحويل بنكي',      NULL, NULL),
    (N'hesham.tawfik@bosla.demo', N'khaled.ali@bosla.demo',       '2026-07-07T09:00:00+02:00', N'Pending', N'بطاقة ائتمان',    NULL, NULL),
    (N'rana.adel@bosla.demo',     N'marwa.said@bosla.demo',       '2026-07-08T11:00:00+02:00', N'Pending', N'محفظة إلكترونية', NULL, NULL),

    -- ======== 1 Cancelled appointment -> Payment Refunded ========
    -- eman.hassan -> mostafa.kamal (confirmed then cancelled)
    (N'eman.hassan@bosla.demo', N'mostafa.kamal@bosla.demo', '2026-06-10T08:00:00+02:00', N'Refunded', N'بطاقة ائتمان', N'bosla_pay_009', '2026-06-10T09:05:00+02:00'),

    -- ======== 2 Pending appointments -> Payment Pending, not yet paid ========
    (N'mohamed.reda@bosla.demo', N'ahmed.hassan@bosla.demo', '2026-07-06T10:00:00+02:00', N'Pending', N'', NULL, NULL),
    (N'eman.hassan@bosla.demo',  N'sara.mohamed@bosla.demo', '2026-07-07T09:00:00+02:00', N'Pending', N'', NULL, NULL)
) AS D(ClientEmail, SpecialistEmail, StartDt, PayStatus, Method, ExtId, PaidAt)
INNER JOIN Appointments a ON a.Start = D.StartDt
INNER JOIN AspNetUsers u ON u.Id = a.UserId AND u.Email = D.ClientEmail
INNER JOIN Specialists sp ON sp.Id = a.SpecialistId
INNER JOIN AspNetUsers su ON su.Id = sp.UserId AND su.Email = D.SpecialistEmail
WHERE NOT EXISTS (SELECT 1 FROM Payments p WHERE p.AppointmentId = a.Id);

PRINT CONCAT(N'Inserted ', @@ROWCOUNT, N' payments.');
GO
