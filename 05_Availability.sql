-- ============================================================
-- 05_Availability.sql
-- Weekly availability slots for all 50 Specialists
-- ============================================================
-- Depends on: 02_Specialists.sql (Specialists table populated)
-- Generates 2-week recurring availability starting next Monday.
-- Each specialist has different schedules.
-- ============================================================

SET NOCOUNT ON;

-- Find next Monday at midnight (local Egypt time = UTC+2)
DECLARE @Today DATE = CAST(SYSDATETIME() AS DATE);
DECLARE @DaysUntilMonday INT = (9 - DATEPART(WEEKDAY, @Today)) % 7;
IF @DaysUntilMonday = 0 SET @DaysUntilMonday = 7;
DECLARE @NextMonday DATETIME2 = DATEADD(DAY, @DaysUntilMonday, CAST(@Today AS DATETIME2));
DECLARE @TwoWeeksLater DATETIME2 = DATEADD(DAY, 14, @NextMonday);

-- ============================================================
-- Helper: generate timeslot range for a given specialist
-- Each slot = 1 hour
-- ============================================================

-- Schedule definitions (email, day_of_week 0=Mon..6=Sun, start_hour, end_hour)
DECLARE @Schedules TABLE (Email NVARCHAR(256), WeekDay INT, StartHr INT, EndHr INT);

INSERT INTO @Schedules
VALUES
    -- Backend Engineers: Sun-Thu, morning/afternoon
    (N'ahmed.hassan@bosla.demo',     0, 9, 13), (N'ahmed.hassan@bosla.demo',     0, 14, 17),
    (N'ahmed.hassan@bosla.demo',     1, 9, 13), (N'ahmed.hassan@bosla.demo',     1, 14, 17),
    (N'ahmed.hassan@bosla.demo',     2, 9, 13), (N'ahmed.hassan@bosla.demo',     2, 14, 17),
    (N'ahmed.hassan@bosla.demo',     3, 9, 13), (N'ahmed.hassan@bosla.demo',     3, 14, 17),
    (N'ahmed.hassan@bosla.demo',     4, 9, 13), (N'ahmed.hassan@bosla.demo',     4, 14, 17),

    (N'mona.ibrahim@bosla.demo',      0, 10, 15),
    (N'mona.ibrahim@bosla.demo',      1, 10, 15),
    (N'mona.ibrahim@bosla.demo',      2, 10, 15),
    (N'mona.ibrahim@bosla.demo',      3, 10, 15),
    (N'mona.ibrahim@bosla.demo',      4, 10, 15),

    (N'khaled.ali@bosla.demo',        0, 8, 12),  (N'khaled.ali@bosla.demo',        0, 13, 16),
    (N'khaled.ali@bosla.demo',        1, 8, 12),  (N'khaled.ali@bosla.demo',        1, 13, 16),
    (N'khaled.ali@bosla.demo',        2, 8, 12),  (N'khaled.ali@bosla.demo',        2, 13, 16),
    (N'khaled.ali@bosla.demo',        3, 8, 12),  (N'khaled.ali@bosla.demo',        3, 13, 16),
    (N'khaled.ali@bosla.demo',        4, 8, 12),  (N'khaled.ali@bosla.demo',        4, 13, 16),
    (N'khaled.ali@bosla.demo',        5, 10, 14),

    (N'nour.ahmed@bosla.demo',        0, 11, 16),
    (N'nour.ahmed@bosla.demo',        1, 11, 16),
    (N'nour.ahmed@bosla.demo',        2, 11, 16),
    (N'nour.ahmed@bosla.demo',        3, 11, 16),
    (N'nour.ahmed@bosla.demo',        4, 11, 16),

    (N'youssef.mahmoud@bosla.demo',   0, 9, 14),  (N'youssef.mahmoud@bosla.demo',   0, 15, 18),
    (N'youssef.mahmoud@bosla.demo',   1, 9, 14),  (N'youssef.mahmoud@bosla.demo',   1, 15, 18),
    (N'youssef.mahmoud@bosla.demo',   2, 9, 14),  (N'youssef.mahmoud@bosla.demo',   2, 15, 18),
    (N'youssef.mahmoud@bosla.demo',   3, 9, 14),  (N'youssef.mahmoud@bosla.demo',   3, 15, 18),
    (N'youssef.mahmoud@bosla.demo',   4, 9, 14),  (N'youssef.mahmoud@bosla.demo',   4, 15, 18),
    (N'youssef.mahmoud@bosla.demo',   5, 10, 14),

    (N'salma.hassan@bosla.demo',      1, 12, 17),
    (N'salma.hassan@bosla.demo',      2, 12, 17),
    (N'salma.hassan@bosla.demo',      3, 12, 17),
    (N'salma.hassan@bosla.demo',      4, 12, 17),
    (N'salma.hassan@bosla.demo',      5, 11, 15),

    (N'omar.nabil@bosla.demo',        0, 10, 14), (N'omar.nabil@bosla.demo',        0, 15, 19),
    (N'omar.nabil@bosla.demo',        1, 10, 14), (N'omar.nabil@bosla.demo',        1, 15, 19),
    (N'omar.nabil@bosla.demo',        2, 10, 14), (N'omar.nabil@bosla.demo',        2, 15, 19),
    (N'omar.nabil@bosla.demo',        3, 10, 14), (N'omar.nabil@bosla.demo',        3, 15, 19),
    (N'omar.nabil@bosla.demo',        4, 10, 14), (N'omar.nabil@bosla.demo',        4, 15, 19),

    -- Frontend Engineers: variety - some work Sat+Sun
    (N'sara.mohamed@bosla.demo',      0, 9, 16),
    (N'sara.mohamed@bosla.demo',      1, 9, 16),
    (N'sara.mohamed@bosla.demo',      2, 9, 16),
    (N'sara.mohamed@bosla.demo',      3, 9, 16),
    (N'sara.mohamed@bosla.demo',      4, 9, 16),

    (N'ali.abdelrahman@bosla.demo',   0, 8, 12),  (N'ali.abdelrahman@bosla.demo',   0, 13, 17),
    (N'ali.abdelrahman@bosla.demo',   1, 8, 12),  (N'ali.abdelrahman@bosla.demo',   1, 13, 17),
    (N'ali.abdelrahman@bosla.demo',   2, 8, 12),  (N'ali.abdelrahman@bosla.demo',   2, 13, 17),
    (N'ali.abdelrahman@bosla.demo',   3, 8, 12),  (N'ali.abdelrahman@bosla.demo',   3, 13, 17),
    (N'ali.abdelrahman@bosla.demo',   4, 8, 12),  (N'ali.abdelrahman@bosla.demo',   4, 13, 17),

    (N'mariam.khaled@bosla.demo',     0, 11, 15), (N'mariam.khaled@bosla.demo',     0, 16, 20),
    (N'mariam.khaled@bosla.demo',     2, 11, 15), (N'mariam.khaled@bosla.demo',     2, 16, 20),
    (N'mariam.khaled@bosla.demo',     4, 11, 15), (N'mariam.khaled@bosla.demo',     4, 16, 20),
    (N'mariam.khaled@bosla.demo',     5, 12, 16),

    (N'hossam.ibrahim@bosla.demo',    1, 9, 14),  (N'hossam.ibrahim@bosla.demo',    1, 15, 18),
    (N'hossam.ibrahim@bosla.demo',    2, 9, 14),  (N'hossam.ibrahim@bosla.demo',    2, 15, 18),
    (N'hossam.ibrahim@bosla.demo',    3, 9, 14),  (N'hossam.ibrahim@bosla.demo',    3, 15, 18),
    (N'hossam.ibrahim@bosla.demo',    4, 9, 14),  (N'hossam.ibrahim@bosla.demo',    4, 15, 18),
    (N'hossam.ibrahim@bosla.demo',    6, 9, 13),

    (N'nada.tarek@bosla.demo',        0, 10, 14), (N'nada.tarek@bosla.demo',        0, 15, 18),
    (N'nada.tarek@bosla.demo',        1, 10, 14), (N'nada.tarek@bosla.demo',        1, 15, 18),
    (N'nada.tarek@bosla.demo',        2, 10, 14), (N'nada.tarek@bosla.demo',        2, 15, 18),
    (N'nada.tarek@bosla.demo',        3, 10, 14), (N'nada.tarek@bosla.demo',        3, 15, 18),
    (N'nada.tarek@bosla.demo',        5, 11, 16),

    (N'karim.hassan@bosla.demo',      1, 9, 13),  (N'karim.hassan@bosla.demo',      1, 14, 18),
    (N'karim.hassan@bosla.demo',      2, 9, 13),  (N'karim.hassan@bosla.demo',      2, 14, 18),
    (N'karim.hassan@bosla.demo',      3, 9, 13),  (N'karim.hassan@bosla.demo',      3, 14, 18),
    (N'karim.hassan@bosla.demo',      4, 9, 13),  (N'karim.hassan@bosla.demo',      4, 14, 18),
    (N'karim.hassan@bosla.demo',      5, 10, 14),

    (N'laila.mostafa@bosla.demo',     0, 10, 16),
    (N'laila.mostafa@bosla.demo',     1, 10, 16),
    (N'laila.mostafa@bosla.demo',     2, 10, 16),
    (N'laila.mostafa@bosla.demo',     3, 10, 16),
    (N'laila.mostafa@bosla.demo',     4, 10, 16),

    -- Mobile Developers
    (N'mohamed.samir@bosla.demo',     0, 9, 15),
    (N'mohamed.samir@bosla.demo',     1, 9, 15),
    (N'mohamed.samir@bosla.demo',     2, 9, 15),
    (N'mohamed.samir@bosla.demo',     3, 9, 15),
    (N'mohamed.samir@bosla.demo',     4, 9, 15),

    (N'dina.essam@bosla.demo',        1, 10, 14), (N'dina.essam@bosla.demo',        1, 15, 19),
    (N'dina.essam@bosla.demo',        2, 10, 14), (N'dina.essam@bosla.demo',        2, 15, 19),
    (N'dina.essam@bosla.demo',        3, 10, 14), (N'dina.essam@bosla.demo',        3, 15, 19),
    (N'dina.essam@bosla.demo',        4, 10, 14), (N'dina.essam@bosla.demo',        4, 15, 19),

    (N'amr.elsayed@bosla.demo',       0, 8, 12),  (N'amr.elsayed@bosla.demo',       0, 13, 17),
    (N'amr.elsayed@bosla.demo',       1, 8, 12),  (N'amr.elsayed@bosla.demo',       1, 13, 17),
    (N'amr.elsayed@bosla.demo',       2, 8, 12),  (N'amr.elsayed@bosla.demo',       2, 13, 17),
    (N'amr.elsayed@bosla.demo',       3, 8, 12),  (N'amr.elsayed@bosla.demo',       3, 13, 17),
    (N'amr.elsayed@bosla.demo',       4, 8, 12),  (N'amr.elsayed@bosla.demo',       4, 13, 17),

    (N'yasmin.adel@bosla.demo',       0, 11, 15), (N'yasmin.adel@bosla.demo',       0, 16, 19),
    (N'yasmin.adel@bosla.demo',       2, 11, 15), (N'yasmin.adel@bosla.demo',       2, 16, 19),
    (N'yasmin.adel@bosla.demo',       4, 11, 15), (N'yasmin.adel@bosla.demo',       4, 16, 19),
    (N'yasmin.adel@bosla.demo',       6, 11, 15),

    (N'tamer.naguib@bosla.demo',      0, 9, 14), (N'tamer.naguib@bosla.demo',       0, 15, 18),
    (N'tamer.naguib@bosla.demo',      1, 9, 14), (N'tamer.naguib@bosla.demo',       1, 15, 18),
    (N'tamer.naguib@bosla.demo',      2, 9, 14), (N'tamer.naguib@bosla.demo',       2, 15, 18),
    (N'tamer.naguib@bosla.demo',      3, 9, 14), (N'tamer.naguib@bosla.demo',       3, 15, 18),
    (N'tamer.naguib@bosla.demo',      4, 9, 14), (N'tamer.naguib@bosla.demo',       4, 15, 18),

    (N'hana.youssef@bosla.demo',      0, 9, 13), (N'hana.youssef@bosla.demo',       0, 14, 17),
    (N'hana.youssef@bosla.demo',      1, 9, 13), (N'hana.youssef@bosla.demo',       1, 14, 17),
    (N'hana.youssef@bosla.demo',      2, 9, 13), (N'hana.youssef@bosla.demo',       2, 14, 17),
    (N'hana.youssef@bosla.demo',      3, 9, 13), (N'hana.youssef@bosla.demo',       3, 14, 17),

    -- DevOps Engineers: on-call style, weekend coverage
    (N'mostafa.kamal@bosla.demo',     0, 8, 12), (N'mostafa.kamal@bosla.demo',     0, 13, 17),
    (N'mostafa.kamal@bosla.demo',     1, 8, 12), (N'mostafa.kamal@bosla.demo',     1, 13, 17),
    (N'mostafa.kamal@bosla.demo',     2, 8, 12), (N'mostafa.kamal@bosla.demo',     2, 13, 17),
    (N'mostafa.kamal@bosla.demo',     3, 8, 12), (N'mostafa.kamal@bosla.demo',     3, 13, 17),
    (N'mostafa.kamal@bosla.demo',     4, 8, 12), (N'mostafa.kamal@bosla.demo',     4, 13, 17),
    (N'mostafa.kamal@bosla.demo',     5, 10, 14),

    (N'rania.abdelaziz@bosla.demo',   0, 10, 15),
    (N'rania.abdelaziz@bosla.demo',   1, 10, 15),
    (N'rania.abdelaziz@bosla.demo',   2, 10, 15),
    (N'rania.abdelaziz@bosla.demo',   3, 10, 15),
    (N'rania.abdelaziz@bosla.demo',   6, 11, 16),

    (N'sherif.tawfik@bosla.demo',     0, 9, 12), (N'sherif.tawfik@bosla.demo',     0, 13, 16),
    (N'sherif.tawfik@bosla.demo',     1, 9, 12), (N'sherif.tawfik@bosla.demo',     1, 13, 16),
    (N'sherif.tawfik@bosla.demo',     3, 9, 12), (N'sherif.tawfik@bosla.demo',     3, 13, 16),
    (N'sherif.tawfik@bosla.demo',     4, 9, 12), (N'sherif.tawfik@bosla.demo',     4, 13, 16),

    (N'nihal.rashad@bosla.demo',      1, 12, 17),
    (N'nihal.rashad@bosla.demo',      2, 12, 17),
    (N'nihal.rashad@bosla.demo',      3, 12, 17),
    (N'nihal.rashad@bosla.demo',      4, 12, 17),
    (N'nihal.rashad@bosla.demo',      5, 11, 16),

    (N'ayman.sherif@bosla.demo',      0, 8, 14), (N'ayman.sherif@bosla.demo',      0, 15, 18),
    (N'ayman.sherif@bosla.demo',      1, 8, 14), (N'ayman.sherif@bosla.demo',      1, 15, 18),
    (N'ayman.sherif@bosla.demo',      2, 8, 14), (N'ayman.sherif@bosla.demo',      2, 15, 18),
    (N'ayman.sherif@bosla.demo',      3, 8, 14), (N'ayman.sherif@bosla.demo',      3, 15, 18),
    (N'ayman.sherif@bosla.demo',      4, 8, 14),

    (N'reem.galal@bosla.demo',        0, 9, 13), (N'reem.galal@bosla.demo',        0, 14, 17),
    (N'reem.galal@bosla.demo',        1, 9, 13), (N'reem.galal@bosla.demo',        1, 14, 17),
    (N'reem.galal@bosla.demo',        2, 9, 13), (N'reem.galal@bosla.demo',        2, 14, 17),
    (N'reem.galal@bosla.demo',        5, 9, 13), (N'reem.galal@bosla.demo',        5, 14, 17),
    (N'reem.galal@bosla.demo',        6, 10, 14),

    -- Cloud Architects: various patterns
    (N'tarek.mahmoud@bosla.demo',     0, 8, 12), (N'tarek.mahmoud@bosla.demo',     0, 13, 16),
    (N'tarek.mahmoud@bosla.demo',     1, 8, 12), (N'tarek.mahmoud@bosla.demo',     1, 13, 16),
    (N'tarek.mahmoud@bosla.demo',     2, 8, 12), (N'tarek.mahmoud@bosla.demo',     2, 13, 16),
    (N'tarek.mahmoud@bosla.demo',     3, 8, 12), (N'tarek.mahmoud@bosla.demo',     3, 13, 16),
    (N'tarek.mahmoud@bosla.demo',     4, 8, 12),

    (N'heba.ibrahim@bosla.demo',      0, 10, 14), (N'heba.ibrahim@bosla.demo',     0, 15, 19),
    (N'heba.ibrahim@bosla.demo',      1, 10, 14), (N'heba.ibrahim@bosla.demo',     1, 15, 19),
    (N'heba.ibrahim@bosla.demo',      2, 10, 14), (N'heba.ibrahim@bosla.demo',     2, 15, 19),
    (N'heba.ibrahim@bosla.demo',      3, 10, 14), (N'heba.ibrahim@bosla.demo',     3, 15, 19),

    (N'waleed.hassan@bosla.demo',     0, 9, 15),
    (N'waleed.hassan@bosla.demo',     1, 9, 15),
    (N'waleed.hassan@bosla.demo',     2, 9, 15),
    (N'waleed.hassan@bosla.demo',     3, 9, 15),
    (N'waleed.hassan@bosla.demo',     5, 10, 15),

    (N'marwa.said@bosla.demo',        0, 11, 16), (N'marwa.said@bosla.demo',       0, 17, 20),
    (N'marwa.said@bosla.demo',        2, 11, 16), (N'marwa.said@bosla.demo',       2, 17, 20),
    (N'marwa.said@bosla.demo',        4, 11, 16), (N'marwa.said@bosla.demo',       4, 17, 20),
    (N'marwa.said@bosla.demo',        6, 11, 15),

    (N'islam.ahmed@bosla.demo',       0, 9, 13), (N'islam.ahmed@bosla.demo',      0, 14, 18),
    (N'islam.ahmed@bosla.demo',       1, 9, 13), (N'islam.ahmed@bosla.demo',      1, 14, 18),
    (N'islam.ahmed@bosla.demo',       2, 9, 13), (N'islam.ahmed@bosla.demo',      2, 14, 18),
    (N'islam.ahmed@bosla.demo',       3, 9, 13), (N'islam.ahmed@bosla.demo',      3, 14, 18),
    (N'islam.ahmed@bosla.demo',       4, 9, 13),

    (N'nesrine.khaled@bosla.demo',    1, 10, 15),
    (N'nesrine.khaled@bosla.demo',    2, 10, 15),
    (N'nesrine.khaled@bosla.demo',    3, 10, 15),
    (N'nesrine.khaled@bosla.demo',    4, 10, 15),
    (N'nesrine.khaled@bosla.demo',    5, 11, 16),

    (N'hassan.younis@bosla.demo',     0, 9, 13), (N'hassan.younis@bosla.demo',    0, 14, 17),
    (N'hassan.younis@bosla.demo',     1, 9, 13), (N'hassan.younis@bosla.demo',    1, 14, 17),
    (N'hassan.younis@bosla.demo',     2, 9, 13), (N'hassan.younis@bosla.demo',    2, 14, 17),
    (N'hassan.younis@bosla.demo',     3, 9, 13), (N'hassan.younis@bosla.demo',    3, 14, 17),
    (N'hassan.younis@bosla.demo',     6, 10, 14),

    -- Data Scientists
    (N'nermeen.adel@bosla.demo',      0, 10, 16),
    (N'nermeen.adel@bosla.demo',      1, 10, 16),
    (N'nermeen.adel@bosla.demo',      2, 10, 16),
    (N'nermeen.adel@bosla.demo',      3, 10, 16),
    (N'nermeen.adel@bosla.demo',      4, 10, 16),

    (N'ashraf.khalil@bosla.demo',     0, 9, 14), (N'ashraf.khalil@bosla.demo',    0, 15, 18),
    (N'ashraf.khalil@bosla.demo',     1, 9, 14), (N'ashraf.khalil@bosla.demo',    1, 15, 18),
    (N'ashraf.khalil@bosla.demo',     2, 9, 14), (N'ashraf.khalil@bosla.demo',    2, 15, 18),
    (N'ashraf.khalil@bosla.demo',     3, 9, 14), (N'ashraf.khalil@bosla.demo',    3, 15, 18),
    (N'ashraf.khalil@bosla.demo',     4, 9, 14),

    (N'hala.mahmoud@bosla.demo',      0, 9, 12), (N'hala.mahmoud@bosla.demo',      0, 13, 16),
    (N'hala.mahmoud@bosla.demo',      1, 9, 12), (N'hala.mahmoud@bosla.demo',      1, 13, 16),
    (N'hala.mahmoud@bosla.demo',      2, 9, 12), (N'hala.mahmoud@bosla.demo',      2, 13, 16),
    (N'hala.mahmoud@bosla.demo',      3, 9, 12), (N'hala.mahmoud@bosla.demo',      3, 13, 16),
    (N'hala.mahmoud@bosla.demo',      4, 9, 12),

    (N'bassem.sameh@bosla.demo',      1, 10, 14), (N'bassem.sameh@bosla.demo',     1, 15, 19),
    (N'bassem.sameh@bosla.demo',      2, 10, 14), (N'bassem.sameh@bosla.demo',     2, 15, 19),
    (N'bassem.sameh@bosla.demo',      3, 10, 14), (N'bassem.sameh@bosla.demo',     3, 15, 19),
    (N'bassem.sameh@bosla.demo',      4, 10, 14), (N'bassem.sameh@bosla.demo',     4, 15, 19),
    (N'bassem.sameh@bosla.demo',      5, 10, 14),

    (N'rasha.ezzat@bosla.demo',       0, 10, 15),
    (N'rasha.ezzat@bosla.demo',       1, 10, 15),
    (N'rasha.ezzat@bosla.demo',       2, 10, 15),
    (N'rasha.ezzat@bosla.demo',       3, 10, 15),
    (N'rasha.ezzat@bosla.demo',       6, 11, 16),

    (N'ziad.amr@bosla.demo',          0, 9, 13), (N'ziad.amr@bosla.demo',         0, 14, 18),
    (N'ziad.amr@bosla.demo',          2, 9, 13), (N'ziad.amr@bosla.demo',         2, 14, 18),
    (N'ziad.amr@bosla.demo',          4, 9, 13), (N'ziad.amr@bosla.demo',         4, 14, 18),
    (N'ziad.amr@bosla.demo',          5, 10, 14),

    -- ML Engineers
    (N'abdelrahman.nasser@bosla.demo',0, 8, 12), (N'abdelrahman.nasser@bosla.demo',0, 13, 17),
    (N'abdelrahman.nasser@bosla.demo',1, 8, 12), (N'abdelrahman.nasser@bosla.demo',1, 13, 17),
    (N'abdelrahman.nasser@bosla.demo',2, 8, 12), (N'abdelrahman.nasser@bosla.demo',2, 13, 17),
    (N'abdelrahman.nasser@bosla.demo',3, 8, 12), (N'abdelrahman.nasser@bosla.demo',3, 13, 17),
    (N'abdelrahman.nasser@bosla.demo',4, 8, 12),

    (N'fatma.elzahraa@bosla.demo',    0, 10, 16),
    (N'fatma.elzahraa@bosla.demo',    1, 10, 16),
    (N'fatma.elzahraa@bosla.demo',    2, 10, 16),
    (N'fatma.elzahraa@bosla.demo',    3, 10, 16),
    (N'fatma.elzahraa@bosla.demo',    4, 10, 16),

    (N'mahmoud.sabry@bosla.demo',     0, 9, 14), (N'mahmoud.sabry@bosla.demo',    0, 15, 18),
    (N'mahmoud.sabry@bosla.demo',     1, 9, 14), (N'mahmoud.sabry@bosla.demo',    1, 15, 18),
    (N'mahmoud.sabry@bosla.demo',     2, 9, 14), (N'mahmoud.sabry@bosla.demo',    2, 15, 18),
    (N'mahmoud.sabry@bosla.demo',     3, 9, 14), (N'mahmoud.sabry@bosla.demo',    3, 15, 18),

    (N'aya.yasser@bosla.demo',        1, 10, 14), (N'aya.yasser@bosla.demo',       1, 15, 19),
    (N'aya.yasser@bosla.demo',        2, 10, 14), (N'aya.yasser@bosla.demo',       2, 15, 19),
    (N'aya.yasser@bosla.demo',        4, 10, 14), (N'aya.yasser@bosla.demo',       4, 15, 19),
    (N'aya.yasser@bosla.demo',        5, 11, 15),

    (N'hisham.lotfy@bosla.demo',      0, 9, 12), (N'hisham.lotfy@bosla.demo',     0, 13, 17),
    (N'hisham.lotfy@bosla.demo',      1, 9, 12), (N'hisham.lotfy@bosla.demo',     1, 13, 17),
    (N'hisham.lotfy@bosla.demo',      2, 9, 12), (N'hisham.lotfy@bosla.demo',     2, 13, 17),
    (N'hisham.lotfy@bosla.demo',      3, 9, 12), (N'hisham.lotfy@bosla.demo',     3, 13, 17),
    (N'hisham.lotfy@bosla.demo',      4, 9, 12),

    (N'nadine.emad@bosla.demo',       0, 10, 14), (N'nadine.emad@bosla.demo',      0, 15, 18),
    (N'nadine.emad@bosla.demo',       1, 10, 14), (N'nadine.emad@bosla.demo',      1, 15, 18),
    (N'nadine.emad@bosla.demo',       2, 10, 14), (N'nadine.emad@bosla.demo',      2, 15, 18),
    (N'nadine.emad@bosla.demo',       3, 10, 14),

    -- Cybersecurity Specialists
    (N'galal.ahmed@bosla.demo',       0, 9, 13),  (N'galal.ahmed@bosla.demo',      0, 14, 18),
    (N'galal.ahmed@bosla.demo',       1, 9, 13),  (N'galal.ahmed@bosla.demo',      1, 14, 18),
    (N'galal.ahmed@bosla.demo',       2, 9, 13),  (N'galal.ahmed@bosla.demo',      2, 14, 18),
    (N'galal.ahmed@bosla.demo',       3, 9, 13),  (N'galal.ahmed@bosla.demo',      3, 14, 18),
    (N'galal.ahmed@bosla.demo',       4, 9, 13),

    (N'samah.ibrahim@bosla.demo',     0, 10, 16),
    (N'samah.ibrahim@bosla.demo',     1, 10, 16),
    (N'samah.ibrahim@bosla.demo',     2, 10, 16),
    (N'samah.ibrahim@bosla.demo',     3, 10, 16),
    (N'samah.ibrahim@bosla.demo',     6, 11, 16),

    (N'hany.magdy@bosla.demo',        0, 8, 12),  (N'hany.magdy@bosla.demo',       0, 13, 16),
    (N'hany.magdy@bosla.demo',        1, 8, 12),  (N'hany.magdy@bosla.demo',       1, 13, 16),
    (N'hany.magdy@bosla.demo',        2, 8, 12),  (N'hany.magdy@bosla.demo',       2, 13, 16),
    (N'hany.magdy@bosla.demo',        3, 8, 12),  (N'hany.magdy@bosla.demo',       3, 13, 16),
    (N'hany.magdy@bosla.demo',        4, 8, 12),

    (N'mai.abdelhamid@bosla.demo',    1, 9, 14),  (N'mai.abdelhamid@bosla.demo',   1, 15, 18),
    (N'mai.abdelhamid@bosla.demo',    2, 9, 14),  (N'mai.abdelhamid@bosla.demo',   2, 15, 18),
    (N'mai.abdelhamid@bosla.demo',    3, 9, 14),  (N'mai.abdelhamid@bosla.demo',   3, 15, 18),
    (N'mai.abdelhamid@bosla.demo',    4, 9, 14),  (N'mai.abdelhamid@bosla.demo',   4, 15, 18),

    (N'khaled.elrashidy@bosla.demo',  0, 9, 13),  (N'khaled.elrashidy@bosla.demo', 0, 14, 17),
    (N'khaled.elrashidy@bosla.demo',  1, 9, 13),  (N'khaled.elrashidy@bosla.demo', 1, 14, 17),
    (N'khaled.elrashidy@bosla.demo',  2, 9, 13),  (N'khaled.elrashidy@bosla.demo', 2, 14, 17),
    (N'khaled.elrashidy@bosla.demo',  3, 9, 13),  (N'khaled.elrashidy@bosla.demo', 3, 14, 17),
    (N'khaled.elrashidy@bosla.demo',  5, 10, 14);

-- ============================================================
-- Generate 1-hour slots for 2 weeks
-- ============================================================
INSERT INTO AvailabilitySlots (SpecialistId, Start, [End], IsBooked, CreatedAtUtc)
SELECT
    sp.Id,
    DATEADD(HOUR, s.StartHr, DATEADD(DAY, s.WeekDay, @NextMonday)),
    DATEADD(HOUR, s.EndHr, DATEADD(DAY, s.WeekDay, @NextMonday)),
    0,
    SYSDATETIMEOFFSET()
FROM @Schedules s
INNER JOIN AspNetUsers u ON u.Email = s.Email
INNER JOIN Specialists sp ON sp.UserId = u.Id
WHERE NOT EXISTS (
    SELECT 1 FROM AvailabilitySlots av
    WHERE av.SpecialistId = sp.Id
      AND av.Start >= @NextMonday
      AND av.Start < @TwoWeeksLater
);

PRINT CONCAT(N'Inserted availability slots for the next 2 weeks starting ', @NextMonday);
GO
