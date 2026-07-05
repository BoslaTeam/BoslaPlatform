SET NOCOUNT ON;

PRINT N'Ensuring all Expertises exist (Arabic)...';

INSERT INTO Expertises (Name)
SELECT v.Name FROM (VALUES
    -- Tech
    (N'تطوير Backend'),
    (N'تطوير Frontend'),
    (N'تطوير تطبيقات الجوال'),
    (N'DevOps'),
    (N'الحوسبة السحابية'),
    (N'علم البيانات'),
    (N'تعلم الآلة'),
    (N'الأمن السيبراني'),
    -- Non-tech
    (N'طب عام'),
    (N'طب أسنان'),
    (N'صيدلة'),
    (N'هندسة مدنية'),
    (N'هندسة ميكانيكا'),
    (N'هندسة كهرباء'),
    (N'قانون'),
    (N'محاسبة'),
    (N'تسويق'),
    (N'تدريس'),
    (N'فنون'),
    (N'إدارة أعمال'),
    (N'تغذية ولياقة')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM Expertises e WHERE e.Name = v.Name);

PRINT CONCAT(N'Ensured 21 Expertises', N' exist.');
GO
