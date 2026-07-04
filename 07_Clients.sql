SET NOCOUNT ON;

DECLARE @Users TABLE (Email NVARCHAR(256) NOT NULL PRIMARY KEY, Id UNIQUEIDENTIFIER NOT NULL);

DECLARE @PasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEHJs6XqMglULAsuy+IippcDtU/4nMqpEZ9tp6dKtIjhxenCMHocwcwBH9SZNLQ1UXQ==';

INSERT INTO AspNetUsers (UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, Name, Title, Bio, ProfileImageUrl, Gender, PreferredLanguage, IsActive, Country, CreatedAtUtc)
OUTPUT INSERTED.Email, INSERTED.Id INTO @Users (Email, Id)
SELECT
    Email, UPPER(Email), Email, UPPER(Email), 1,
    @PasswordHash,
    NEWID(), NEWID(),
    Phone, 1, 0, 1, 0,
    FullName, Title, Bio,
    CASE Gender
        WHEN N'M' THEN N'https://randomuser.me/api/portraits/men/' + CAST(ABS(CHECKSUM(Email)) % 100 AS NVARCHAR) + N'.jpg'
        ELSE N'https://randomuser.me/api/portraits/women/' + CAST(ABS(CHECKSUM(Email)) % 100 AS NVARCHAR) + N'.jpg'
    END,
    Gender, Lang, 1, Country,
    SYSDATETIMEOFFSET()
FROM (VALUES
    (N'mohamed.reda@bosla.demo',  N'02 0110 001 0001', N'محمد رضا',  N'رائد أعمال',  N'رائد أعمال في مجال التكنولوجيا، مهتم بالتعلم والتطوير المستمر.', N'M', N'ar', N'Egypt'),
    (N'eman.hassan@bosla.demo',   N'02 0110 001 0002', N'إيمان حسن', N'مديرة تسويق', N'مديرة تسويق بخبرة ١٢ سنة في الشركات الناشئة.', N'F', N'ar', N'Egypt'),
    (N'ahmed.adel@bosla.demo',    N'02 0110 001 0003', N'أحمد عادل', N'محلل مالي',   N'محلل مالي في بنك استثماري، مهتم بتطوير الذات.', N'M', N'ar', N'Egypt'),
    (N'samia.nabil@bosla.demo',   N'02 0110 001 0004', N'سامية نبيل', N'طبيبة',      N'طبيبة بشرية، تبحث عن استشارات في التكنولوجيا الطبية.', N'F', N'ar', N'Egypt'),
    (N'khaled.mohsen@bosla.demo', N'02 0110 001 0005', N'خالد محسن', N'مهندس مدني',  N'مهندس مدني يرغب في تطوير مهاراته في إدارة المشاريع.', N'M', N'ar', N'Egypt'),
    (N'noha.ashraf@bosla.demo',   N'02 0110 001 0006', N'نهى أشرف',  N'مدرسة',       N'مدرسة لغة عربية، مهتمة بتعلم طرق التدريس الحديثة.', N'F', N'ar', N'Egypt'),
    (N'yasser.ibrahim@bosla.demo',N'02 0110 001 0007', N'ياسر إبراهيم', N'صاحب شركة', N'صاحب شركة مقاولات، يبحث عن استشارات في التحول الرقمي.', N'M', N'ar', N'Egypt'),
    (N'laila.ahmed@bosla.demo',   N'02 0110 001 0008', N'ليلى أحمد', N'مصممة جرافيك', N'مصممة جرافيك مستقلة، تريد تطوير مهاراتها في التسويق.', N'F', N'ar', N'Egypt'),
    (N'hesham.tawfik@bosla.demo', N'02 0110 001 0009', N'هشام توفيق', N'محامٍ',      N'محامٍ متخصص في القانون التجاري، مهتم بالابتكار.', N'M', N'en', N'Egypt'),
    (N'rana.adel@bosla.demo',     N'02 0110 001 0010', N'رنا عادل',  N'طالبة جامعية', N'طالبة في السنة النهائية بكلية الحاسبات، تبحث عن مرشد.', N'F', N'ar', N'Egypt')
) AS U(Email, Phone, FullName, Title, Bio, Gender, Lang, Country)
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = U.Email);

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, (SELECT Id FROM AspNetRoles WHERE Name = 'User')
FROM @Users u
WHERE NOT EXISTS (SELECT 1 FROM AspNetUserRoles r WHERE r.UserId = u.Id);

DECLARE @InsertedCount INT;
SELECT @InsertedCount = COUNT(*) FROM @Users;
PRINT CONCAT(N'Inserted ', @InsertedCount, N' client users.');
GO
