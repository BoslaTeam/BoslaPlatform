-- ============================================================
-- 01_Users.sql
-- 50 Specialist Users (Tech + Non-Tech) for Bosla Platform Demo
-- ============================================================

SET NOCOUNT ON;

DECLARE @SpecialistRoleId UNIQUEIDENTIFIER;
SELECT @SpecialistRoleId = Id FROM AspNetRoles WHERE Name = N'Specialist';
IF @SpecialistRoleId IS NULL
BEGIN
    RAISERROR(N'Specialist role not found. Seed AspNetRoles first.', 16, 1);
    RETURN;
END

DECLARE @Users TABLE (Email NVARCHAR(256) NOT NULL PRIMARY KEY, Id UNIQUEIDENTIFIER NOT NULL);

DECLARE @PasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEHJs6XqMglULAsuy+IippcDtU/4nMqpEZ9tp6dKtIjhxenCMHocwcwBH9SZNLQ1UXQ==';
--P@ssw0rd123

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
    -- ======================== TECH (24) ========================
    -- Backend Development (3)
    (N'ahmed.hassan@bosla.demo',      N'02 0105 001 0001', N'أحمد حسن',        N'استشاري تطوير برمجيات',     N'خبير في هندسة البرمجيات مع 10 سنوات في تصميم الأنظمة الخلفية باستخدام .NET و C#.', N'M', N'ar', N'Egypt'),
    (N'mona.ibrahim@bosla.demo',      N'02 0105 001 0002', N'منى إبراهيم',      N'مهندسة برمجيات خلفية',      N'مهندسة برمجيات متخصصة في تطوير APIs باستخدام ASP.NET Core و EF Core.', N'F', N'ar', N'Egypt'),
    (N'nour.ahmed@bosla.demo',        N'02 0105 001 0003', N'نور أحمد',         N'مطورة تطبيقات جافا',        N'مطورة Java Backend بخبرة في Spring Boot و Microservices.', N'F', N'ar', N'Egypt'),
    -- Frontend Development (3)
    (N'sara.mohamed@bosla.demo',      N'02 0105 001 0004', N'سارة محمد',        N'مهندسة واجهات أمامية',      N'مهندسة Frontend محترفة مع 9 سنوات خبرة في Angular و React.', N'F', N'ar', N'Egypt'),
    (N'ali.abdelrahman@bosla.demo',   N'02 0105 001 0005', N'علي عبدالرحمن',    N'مطور Angular',              N'مطور Angular محترف مع خبرة في RxJS و NgRx.', N'M', N'ar', N'Egypt'),
    (N'hossam.ibrahim@bosla.demo',    N'02 0105 001 0006', N'حسام إبراهيم',      N'مطور Vue.js',               N'مطور Frontend خبير في Vue.js و Pinia.', N'M', N'ar', N'Egypt'),
    -- Mobile Development (3)
    (N'mohamed.samir@bosla.demo',     N'02 0105 001 0007', N'محمد سمير',         N'مطور Flutter',              N'مطور تطبيقات جوال متخصص في Flutter و Dart.', N'M', N'ar', N'Egypt'),
    (N'dina.essam@bosla.demo',        N'02 0105 001 0008', N'دينا عصام',         N'مطورة React Native',        N'مطورة تطبيقات جوال خبيرة في React Native و Expo.', N'F', N'en', N'Egypt'),
    (N'yasmin.adel@bosla.demo',       N'02 0105 001 0009', N'ياسمين عادل',       N'مطورة Android',             N'Android Developer متخصصة في Kotlin و Jetpack Compose.', N'F', N'ar', N'Egypt'),
    -- DevOps (3)
    (N'mostafa.kamal@bosla.demo',     N'02 0105 001 0010', N'مصطفى كمال',        N'مهندس DevOps',              N'مهندس DevOps خبير في CI/CD و Docker و Kubernetes.', N'M', N'ar', N'Egypt'),
    (N'rania.abdelaziz@bosla.demo',   N'02 0105 001 0011', N'رانيا عبدالعزيز',    N'مهندسة DevOps أولى',        N'مهندسة DevOps محترفة مع 10 سنوات في Azure DevOps و Terraform.', N'F', N'ar', N'Egypt'),
    (N'ayman.sherif@bosla.demo',      N'02 0105 001 0012', N'أيمن شريف',         N'مهندس SRE',                 N'Site Reliability Engineer في إدارة الأنظمة عالية التوفر.', N'M', N'ar', N'Egypt'),
    -- Cloud Computing (3)
    (N'tarek.mahmoud@bosla.demo',     N'02 0105 001 0013', N'طارق محمود',        N'معماري AWS',                N'AWS Solutions Architect مع 14 سنة في تصميم البنية التحتية السحابية.', N'M', N'ar', N'Egypt'),
    (N'heba.ibrahim@bosla.demo',      N'02 0105 001 0014', N'هبة إبراهيم',       N'معمارية Azure',             N'Azure Cloud Architect مع 11 سنة في حلول المؤسسات السحابية.', N'F', N'ar', N'Egypt'),
    (N'waleed.hassan@bosla.demo',     N'02 0105 001 0015', N'وليد حسن',          N'معماري Google Cloud',       N'Google Cloud Architect محترف في GCP و Kubernetes.', N'M', N'en', N'Egypt'),
    -- Data Science (3)
    (N'nermeen.adel@bosla.demo',      N'02 0105 001 0016', N'نرمين عادل',        N'عالمة بيانات',              N'Data Scientist في تحليل البيانات الضخمة باستخدام Python و R.', N'F', N'ar', N'Egypt'),
    (N'ashraf.khalil@bosla.demo',     N'02 0105 001 0017', N'أشرف خليل',         N'عالم بيانات أول',           N'Senior Data Scientist في التحليلات التنبؤية وتصور البيانات.', N'M', N'ar', N'Egypt'),
    (N'bassem.sameh@bosla.demo',      N'02 0105 001 0018', N'باسم سامح',         N'مهندس بيانات ضخمة',         N'Big Data Engineer متخصص في Hadoop و Spark و Kafka.', N'M', N'ar', N'Egypt'),
    -- Machine Learning (3)
    (N'abdelrahman.nasser@bosla.demo',N'02 0105 001 0019', N'عبدالرحمن ناصر',    N'مهندس تعلم آلة',            N'ML Engineer في بناء ونشر نماذج TensorFlow و PyTorch.', N'M', N'ar', N'Egypt'),
    (N'fatma.elzahraa@bosla.demo',    N'02 0105 001 0020', N'فاطمة الزهراء',      N'مهندسة تعلم عميق',          N'Deep Learning Engineer في الشبكات العصبية ومعالجة الصور.', N'F', N'ar', N'Egypt'),
    (N'mahmoud.sabry@bosla.demo',     N'02 0105 001 0021', N'محمود صبري',        N'أخصائي NLP',               N'NLP Specialist في معالجة اللغات الطبيعية و LLMs.', N'M', N'ar', N'Egypt'),
    -- Cybersecurity (3)
    (N'galal.ahmed@bosla.demo',       N'02 0105 001 0022', N'جلال أحمد',         N'محلل أمن سيبراني',          N'Cybersecurity Analyst في تحليل الثغرات واختبار الاختراق.', N'M', N'ar', N'Egypt'),
    (N'samah.ibrahim@bosla.demo',     N'02 0105 001 0023', N'سامح إبراهيم',       N'خبير اختبار اختراق',        N'Penetration Tester معتمد (OSCP, CEH).', N'M', N'ar', N'Egypt'),
    (N'khaled.elrashidy@bosla.demo',  N'02 0105 001 0024', N'خالد الرشيدي',       N'أخصائي حوكمة وامتثال',      N'GRC Specialist في ISO 27001 و NIST.', N'M', N'ar', N'Egypt'),
    -- ======================== NON-TECH (26) ========================
    -- طب عام (2)
    (N'omar.nabil@bosla.demo',        N'02 0105 002 0025', N'عمر نبيل',          N'طبيب عام',                  N'طبيب عام مع 12 سنة خبرة في التشخيص والعلاج في مستشفيات كبرى.', N'M', N'ar', N'Egypt'),
    (N'hana.youssef@bosla.demo',      N'02 0105 002 0026', N'هنا يوسف',          N'طبيبة عامة',                N'طبيبة عامة متخصصة في الرعاية الأولية وصحة الأسرة.', N'F', N'en', N'Egypt'),
    -- طب أسنان (2)
    (N'khaled.ali@bosla.demo',        N'02 0105 002 0027', N'خالد علي',          N'طبيب أسنان',                N'طبيب أسنان مع 14 سنة خبرة في زراعة وتجميل الأسنان.', N'M', N'ar', N'Egypt'),
    (N'mariam.karim@bosla.demo',      N'02 0105 002 0028', N'مريم كريم',         N'طبيبة أسنان',               N'طبيبة أسنان متخصصة في تقويم الأسنان وعلاج العصب.', N'F', N'ar', N'Egypt'),
    -- صيدلة (2)
    (N'karim.hassan@bosla.demo',      N'02 0105 002 0029', N'كريم حسن',          N'صيدلي',                     N'صيدلي إكلينيكي مع 7 سنوات في الصيدلة السريرية والمستشفيات.', N'M', N'en', N'Egypt'),
    (N'nada.tarek@bosla.demo',        N'02 0105 002 0030', N'ندى طارق',          N'صيدلانية',                  N'صيدلانية متخصصة في التركيبات الصيدلانية والتوعية الدوائية.', N'F', N'ar', N'Egypt'),
    -- هندسة مدنية (2)
    (N'laila.mostafa@bosla.demo',     N'02 0105 002 0031', N'ليلى مصطفى',        N'مهندسة مدنية',               N'مهندسة مدنية متخصصة في التصميم الإنشائي وإدارة المشاريع.', N'F', N'ar', N'Egypt'),
    (N'amr.elsayed@bosla.demo',       N'02 0105 002 0032', N'عمرو السيد',        N'مهندس مدني',                N'مهندس مدني خبير في الطرق والكباري ومشاريع البنية التحتية.', N'M', N'ar', N'Egypt'),
    -- هندسة ميكانيكا (2)
    (N'tamer.naguib@bosla.demo',      N'02 0105 002 0033', N'تامر نجيب',         N'مهندس ميكانيكا',            N'مهندس ميكانيكا مع 12 سنة في التصميم الميكانيكي والتبريد.', N'M', N'ar', N'Egypt'),
    (N'sherif.tawfik@bosla.demo',     N'02 0105 002 0034', N'شريف توفيق',        N'مهندس ميكانيكا',            N'مهندس ميكانيكا متخصص في أنظمة الطاقة والمحركات.', N'M', N'ar', N'Egypt'),
    -- هندسة كهرباء (2)
    (N'reem.galal@bosla.demo',        N'02 0105 002 0035', N'ريم جلال',          N'مهندسة كهرباء',              N'مهندسة كهرباء متخصصة في أنظمة التحكم الآلي والطاقة.', N'F', N'en', N'Egypt'),
    (N'hala.mahmoud@bosla.demo',      N'02 0105 002 0036', N'هالة محمود',        N'مهندسة كهرباء',              N'مهندسة كهرباء خبيرة في تصميم الشبكات الكهربائية.', N'F', N'ar', N'Egypt'),
    -- قانون (3)
    (N'nihal.rashad@bosla.demo',      N'02 0105 002 0037', N'نهال رشاد',         N'محامية',                     N'محامية متخصصة في القانون التجاري والتحكيم الدولي.', N'F', N'ar', N'Egypt'),
    (N'rasha.ezzat@bosla.demo',       N'02 0105 002 0038', N'رشا عزت',           N'مستشارة قانونية',            N'مستشارة قانونية مع 9 سنوات في صياغة العقود والترافع.', N'F', N'ar', N'Egypt'),
    (N'ziad.amro@bosla.demo',         N'02 0105 002 0039', N'زياد عمرو',         N'محامي',                      N'محامي متخصص في القانون المدني والتجاري.', N'M', N'en', N'Egypt'),
    -- محاسبة (2)
    (N'mai.abdelhamid@bosla.demo',    N'02 0105 002 0040', N'ماي عبدالحميد',     N'مدققة مالية',                N'مدققة مالية مع 6 سنوات في المراجعة والتدقيق المالي.', N'F', N'ar', N'Egypt'),
    (N'islam.ahmed@bosla.demo',       N'02 0105 002 0041', N'إسلام أحمد',        N'محاسب قانوني',               N'محاسب قانوني مع 10 سنوات في الإدارة المالية والضرائب.', N'M', N'ar', N'Egypt'),
    -- تسويق (2)
    (N'marwa.said@bosla.demo',        N'02 0105 002 0042', N'مروة سعيد',         N'أخصائية تسويق',              N'أخصائية تسويق رقمي مع 13 سنة في إ strategiيات التسويق الإلكتروني.', N'F', N'ar', N'Egypt'),
    (N'nesrine.khaled@bosla.demo',    N'02 0105 002 0043', N'نسرين خالد',        N'مديرة تسويق',                N'مديرة تسويق متخصصة في بناء العلامات التجارية والحملات الإعلانية.', N'F', N'en', N'Egypt'),
    -- تدريس (2)
    (N'hassan.younis@bosla.demo',     N'02 0105 002 0044', N'حسن يونس',          N'مدرس لغة عربية',             N'مدرس لغة عربية مع 8 سنوات في تدريس النحو والأدب.', N'M', N'ar', N'Egypt'),
    (N'aya.yasser@bosla.demo',        N'02 0105 002 0045', N'آية ياسر',          N'معلمة رياضيات',              N'معلمة رياضيات متخصصة في تدريس المرحلة الثانوية والجامعية.', N'F', N'ar', N'Egypt'),
    -- فنون (2)
    (N'hisham.lotfy@bosla.demo',      N'02 0105 002 0046', N'هشام لطفي',         N'فنان تشكيلي',                N'فنان تشكيلي مع 9 سنوات في الرسم الزيتي والنحت.', N'M', N'ar', N'Egypt'),
    (N'nadine.emad@bosla.demo',       N'02 0105 002 0047', N'نادين عماد',        N'مصممة جرافيك',               N'مصممة جرافيك متخصصة في الهوية البصرية وتصميم الإعلانات.', N'F', N'en', N'Egypt'),
    -- إدارة أعمال (2)
    (N'hany.magdy@bosla.demo',        N'02 0105 002 0048', N'هاني مجدي',         N'مدير تنفيذي',                N'مدير تنفيذي مع 15 سنة في إدارة الشركات والتخطيط الاستراتيجي.', N'M', N'ar', N'Egypt'),
    (N'salma.hassan@bosla.demo',      N'02 0105 002 0049', N'سلمى حسن',          N'مستشارة إدارة أعمال',        N'مستشارة إدارة أعمال متخصصة في تطوير المؤسسات والقيادة.', N'F', N'ar', N'Egypt'),
    -- تغذية ولياقة (2)
    (N'youssef.mahmoud@bosla.demo',   N'02 0105 002 0050', N'يوسف محمود',        N'مدرب لياقة وتغذية',          N'مدرب لياقة مع 8 سنوات في التغذية الرياضية وبرامج اللياقة.', N'M', N'ar', N'Egypt')
) AS U(Email, Phone, FullName, Title, Bio, Gender, Lang, Country)
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = U.Email);

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, @SpecialistRoleId
FROM @Users u
WHERE NOT EXISTS (SELECT 1 FROM AspNetUserRoles r WHERE r.UserId = u.Id AND r.RoleId = @SpecialistRoleId);

DECLARE @InsertedUserCount INT;
SELECT @InsertedUserCount = COUNT(*) FROM @Users;
PRINT CONCAT(N'Inserted ', @InsertedUserCount, N' specialist users and assigned Specialist role.');
GO
