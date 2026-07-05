SET NOCOUNT ON;

INSERT INTO Educations (UserId, InstitutionName, FieldOfStudy, StartDate, EndDate, CreatedAtUtc)
SELECT u.Id, Institution, Field, StartDt, EndDt, SYSDATETIMEOFFSET()
FROM (VALUES
    (N'ahmed.hassan@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2008-09-01', '2012-06-30'),
    (N'ahmed.hassan@bosla.demo', N'Microsoft Certified: Azure Solutions Architect', N'Cloud Computing', '2020-03-15', NULL),
    (N'ahmed.hassan@bosla.demo', N'Nile University', N'Computer Science', '2014-09-01', '2016-06-30'),

    (N'mona.ibrahim@bosla.demo', N'جامعة عين شمس', N'هندسة', '2009-09-01', '2013-06-30'),
    (N'mona.ibrahim@bosla.demo', N'AWS Certified Solutions Architect', N'Cloud Computing', '2021-05-20', NULL),
    (N'mona.ibrahim@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2022-08-10', NULL),

    (N'nour.ahmed@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2010-09-01', '2014-06-30'),
    (N'nour.ahmed@bosla.demo', N'Microsoft Certified: Azure Fundamentals', N'Cloud Computing', '2019-11-15', NULL),
    (N'nour.ahmed@bosla.demo', N'Nile University', N'Computer Science', '2015-09-01', '2017-06-30'),

    (N'sara.mohamed@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2011-09-01', '2015-06-30'),
    (N'sara.mohamed@bosla.demo', N'Google Cloud Associate Engineer', N'Cloud Computing', '2021-02-10', NULL),
    (N'sara.mohamed@bosla.demo', N'AUC', N'Computer Science', '2016-09-01', '2018-06-30'),

    (N'ali.abdelrahman@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2007-09-01', '2011-06-30'),
    (N'ali.abdelrahman@bosla.demo', N'AWS Certified Developer', N'Cloud Computing', '2020-07-20', NULL),
    (N'ali.abdelrahman@bosla.demo', N'جامعة عين شمس', N'هندسة', '2012-09-01', '2014-06-30'),

    (N'hossam.ibrahim@bosla.demo', N'جامعة عين شمس', N'هندسة', '2006-09-01', '2010-06-30'),
    (N'hossam.ibrahim@bosla.demo', N'Microsoft Certified: DevOps Engineer', N'Cloud Computing', '2021-04-15', NULL),
    (N'hossam.ibrahim@bosla.demo', N'جامعة الإسكندرية', N'هندسة', '2011-09-01', '2013-06-30'),

    (N'mohamed.samir@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2009-09-01', '2013-06-30'),
    (N'mohamed.samir@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2022-01-20', NULL),
    (N'mohamed.samir@bosla.demo', N'AUC', N'Computer Science', '2014-09-01', '2016-06-30'),

    (N'dina.essam@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2012-09-01', '2016-06-30'),
    (N'dina.essam@bosla.demo', N'Microsoft Certified: Azure Solutions Architect', N'Cloud Computing', '2021-08-20', NULL),
    (N'dina.essam@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2017-09-01', '2019-06-30'),

    (N'yasmin.adel@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2010-09-01', '2014-06-30'),
    (N'yasmin.adel@bosla.demo', N'AWS Certified Solutions Architect', N'Cloud Computing', '2019-06-15', NULL),
    (N'yasmin.adel@bosla.demo', N'Nile University', N'Computer Science', '2015-09-01', '2017-06-30'),

    (N'mostafa.kamal@bosla.demo', N'جامعة عين شمس', N'هندسة', '2008-09-01', '2012-06-30'),
    (N'mostafa.kamal@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2021-03-15', NULL),
    (N'mostafa.kamal@bosla.demo', N'Microsoft Certified: Azure Administrator', N'Cloud Computing', '2020-11-10', NULL),

    (N'rania.abdelaziz@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2009-09-01', '2013-06-30'),
    (N'rania.abdelaziz@bosla.demo', N'AWS Certified Developer', N'Cloud Computing', '2020-09-20', NULL),
    (N'rania.abdelaziz@bosla.demo', N'AUC', N'Computer Science', '2014-09-01', '2016-06-30'),

    (N'ayman.sherif@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2007-09-01', '2011-06-30'),
    (N'ayman.sherif@bosla.demo', N'Microsoft Certified: Azure Solutions Architect', N'Cloud Computing', '2022-02-20', NULL),
    (N'ayman.sherif@bosla.demo', N'Nile University', N'Computer Science', '2012-09-01', '2014-06-30'),

    (N'tarek.mahmoud@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2011-09-01', '2015-06-30'),
    (N'tarek.mahmoud@bosla.demo', N'Google Cloud Associate Engineer', N'Cloud Computing', '2020-05-15', NULL),
    (N'tarek.mahmoud@bosla.demo', N'جامعة عين شمس', N'هندسة', '2016-09-01', '2018-06-30'),

    (N'heba.ibrahim@bosla.demo', N'جامعة القاهرة', N'طب', '2007-09-01', '2012-06-30'),
    (N'heba.ibrahim@bosla.demo', N'Board Certification in Internal Medicine', N'Medicine', '2014-03-15', NULL),

    (N'waleed.hassan@bosla.demo', N'جامعة عين شمس', N'هندسة', '2005-09-01', '2009-06-30'),
    (N'waleed.hassan@bosla.demo', N'AWS Certified Solutions Architect', N'Cloud Computing', '2019-04-20', NULL),
    (N'waleed.hassan@bosla.demo', N'AUC', N'Computer Science', '2010-09-01', '2012-06-30'),

    (N'nermeen.adel@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2012-09-01', '2016-06-30'),
    (N'nermeen.adel@bosla.demo', N'Microsoft Certified: Azure Fundamentals', N'Cloud Computing', '2020-07-15', NULL),
    (N'nermeen.adel@bosla.demo', N'Nile University', N'Computer Science', '2017-09-01', '2019-06-30'),

    (N'ashraf.khalil@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2006-09-01', '2010-06-30'),
    (N'ashraf.khalil@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2021-06-20', NULL),
    (N'ashraf.khalil@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2011-09-01', '2013-06-30'),

    (N'bassem.sameh@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2009-09-01', '2013-06-30'),
    (N'bassem.sameh@bosla.demo', N'AWS Certified Developer', N'Cloud Computing', '2022-03-15', NULL),
    (N'bassem.sameh@bosla.demo', N'AUC', N'Computer Science', '2014-09-01', '2016-06-30'),

    (N'abdelrahman.nasser@bosla.demo', N'جامعة عين شمس', N'هندسة', '2010-09-01', '2014-06-30'),
    (N'abdelrahman.nasser@bosla.demo', N'Microsoft Certified: Azure Solutions Architect', N'Cloud Computing', '2021-09-20', NULL),
    (N'abdelrahman.nasser@bosla.demo', N'Nile University', N'Computer Science', '2015-09-01', '2017-06-30'),

    (N'fatma.elzahraa@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2011-09-01', '2015-06-30'),
    (N'fatma.elzahraa@bosla.demo', N'Google Cloud Associate Engineer', N'Cloud Computing', '2020-10-15', NULL),
    (N'fatma.elzahraa@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2016-09-01', '2018-06-30'),

    (N'mahmoud.sabry@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2008-09-01', '2012-06-30'),
    (N'mahmoud.sabry@bosla.demo', N'AWS Certified Solutions Architect', N'Cloud Computing', '2019-08-20', NULL),
    (N'mahmoud.sabry@bosla.demo', N'جامعة عين شمس', N'هندسة', '2013-09-01', '2015-06-30'),

    (N'galal.ahmed@bosla.demo', N'جامعة عين شمس', N'هندسة', '2007-09-01', '2011-06-30'),
    (N'galal.ahmed@bosla.demo', N'Microsoft Certified: Azure Administrator', N'Cloud Computing', '2020-04-15', NULL),
    (N'galal.ahmed@bosla.demo', N'AUC', N'Computer Science', '2012-09-01', '2014-06-30'),

    (N'samah.ibrahim@bosla.demo', N'جامعة القاهرة', N'صيدلة', '2009-09-01', '2013-06-30'),
    (N'samah.ibrahim@bosla.demo', N'Egyptian Pharmacist License', N'Pharmacy', '2014-01-15', NULL),

    (N'khaled.elrashidy@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2006-09-01', '2010-06-30'),
    (N'khaled.elrashidy@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2021-07-20', NULL),
    (N'khaled.elrashidy@bosla.demo', N'Nile University', N'Computer Science', '2011-09-01', '2013-06-30'),

    (N'omar.nabil@bosla.demo', N'جامعة المنصورة', N'حاسبات', '2013-09-01', '2017-06-30'),
    (N'omar.nabil@bosla.demo', N'AWS Certified Developer', N'Cloud Computing', '2022-05-15', NULL),
    (N'omar.nabil@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2018-09-01', '2020-06-30'),

    (N'hana.youssef@bosla.demo', N'جامعة القاهرة', N'حاسبات ومعلومات', '2009-09-01', '2013-06-30'),
    (N'hana.youssef@bosla.demo', N'Microsoft Certified: Azure Solutions Architect', N'Cloud Computing', '2021-11-20', NULL),
    (N'hana.youssef@bosla.demo', N'جامعة الإسكندرية', N'علوم', '2014-09-01', '2016-06-30'),

    (N'khaled.ali@bosla.demo', N'جامعة عين شمس', N'هندسة', '2008-09-01', '2012-06-30'),
    (N'khaled.ali@bosla.demo', N'Google Cloud Professional', N'Cloud Computing', '2020-12-15', NULL),
    (N'khaled.ali@bosla.demo', N'AUC', N'Computer Science', '2013-09-01', '2015-06-30'),

    (N'mariam.karim@bosla.demo', N'جامعة القاهرة', N'طب', '2005-09-01', '2010-06-30'),
    (N'mariam.karim@bosla.demo', N'جامعة عين شمس', N'طب', '2011-09-01', '2014-06-30'),

    (N'karim.hassan@bosla.demo', N'جامعة عين شمس', N'صيدلة', '2008-09-01', '2012-06-30'),
    (N'karim.hassan@bosla.demo', N'جامعة المنصورة', N'صيدلة', '2013-09-01', '2015-06-30'),

    (N'nada.tarek@bosla.demo', N'جامعة الإسكندرية', N'طب', '2010-09-01', '2015-06-30'),
    (N'nada.tarek@bosla.demo', N'Board Certification in Pediatrics', N'Medicine', '2017-03-15', NULL),

    (N'laila.mostafa@bosla.demo', N'جامعة القاهرة', N'صيدلة', '2010-09-01', '2014-06-30'),
    (N'laila.mostafa@bosla.demo', N'Clinical Pharmacy Diploma', N'Pharmacy', '2015-09-01', '2016-06-30'),

    (N'amr.elsayed@bosla.demo', N'جامعة عين شمس', N'هندسة', '2005-09-01', '2009-06-30'),
    (N'amr.elsayed@bosla.demo', N'جامعة القاهرة', N'هندسة', '2010-09-01', '2012-06-30'),

    (N'tamer.naguib@bosla.demo', N'جامعة القاهرة', N'حقوق', '2006-09-01', '2010-06-30'),
    (N'tamer.naguib@bosla.demo', N'جامعة الزقازيق', N'حقوق', '2011-09-01', '2013-06-30'),

    (N'sherif.tawfik@bosla.demo', N'جامعة عين شمس', N'هندسة', '2007-09-01', '2011-06-30'),
    (N'sherif.tawfik@bosla.demo', N'جامعة الإسكندرية', N'هندسة', '2012-09-01', '2014-06-30'),

    (N'reem.galal@bosla.demo', N'جامعة القاهرة', N'تجارة', '2009-09-01', '2013-06-30'),
    (N'reem.galal@bosla.demo', N'جامعة عين شمس', N'تجارة', '2014-09-01', '2016-06-30'),

    (N'hala.mahmoud@bosla.demo', N'AUC', N'Business', '2008-09-01', '2012-06-30'),
    (N'hala.mahmoud@bosla.demo', N'جامعة القاهرة', N'إعلام', '2013-09-01', '2015-06-30'),

    (N'nihal.rashad@bosla.demo', N'جامعة القاهرة', N'تربية', '2009-09-01', '2013-06-30'),
    (N'nihal.rashad@bosla.demo', N'جامعة الأزهر', N'لغة عربية', '2014-09-01', '2016-06-30'),

    (N'rasha.ezzat@bosla.demo', N'جامعة حلوان', N'فنون جميلة', '2010-09-01', '2014-06-30'),
    (N'rasha.ezzat@bosla.demo', N'كلية الفنون التطبيقية', N'فنون تطبيقية', '2015-09-01', '2017-06-30'),

    (N'ziad.amro@bosla.demo', N'AUC', N'MBA', '2011-09-01', '2013-06-30'),
    (N'ziad.amro@bosla.demo', N'جامعة القاهرة', N'إدارة أعمال', '2006-09-01', '2010-06-30'),

    (N'mai.abdelhamid@bosla.demo', N'جامعة القاهرة', N'طب', '2008-09-01', '2013-06-30'),
    (N'mai.abdelhamid@bosla.demo', N'جامعة الإسكندرية', N'طب', '2014-09-01', '2016-06-30'),

    (N'islam.ahmed@bosla.demo', N'جامعة المنصورة', N'صيدلة', '2007-09-01', '2011-06-30'),
    (N'islam.ahmed@bosla.demo', N'جامعة القاهرة', N'صيدلة', '2012-09-01', '2014-06-30'),

    (N'marwa.said@bosla.demo', N'جامعة القاهرة', N'حقوق', '2009-09-01', '2013-06-30'),
    (N'marwa.said@bosla.demo', N'جامعة عين شمس', N'حقوق', '2014-09-01', '2016-06-30'),

    (N'nesrine.khaled@bosla.demo', N'جامعة القاهرة', N'تجارة', '2008-09-01', '2012-06-30'),
    (N'nesrine.khaled@bosla.demo', N'AUC', N'Business', '2013-09-01', '2015-06-30'),

    (N'hassan.younis@bosla.demo', N'جامعة القاهرة', N'هندسة', '2006-09-01', '2010-06-30'),
    (N'hassan.younis@bosla.demo', N'جامعة عين شمس', N'هندسة', '2011-09-01', '2013-06-30'),

    (N'aya.yasser@bosla.demo', N'جامعة حلوان', N'فنون جميلة', '2011-09-01', '2015-06-30'),
    (N'aya.yasser@bosla.demo', N'كلية الفنون التطبيقية', N'فنون تطبيقية', '2016-09-01', '2018-06-30'),

    (N'hisham.lotfy@bosla.demo', N'جامعة القاهرة', N'حقوق', '2005-09-01', '2009-06-30'),
    (N'hisham.lotfy@bosla.demo', N'جامعة الزقازيق', N'حقوق', '2010-09-01', '2012-06-30'),

    (N'nadine.emad@bosla.demo', N'AUC', N'MBA', '2010-09-01', '2012-06-30'),
    (N'nadine.emad@bosla.demo', N'جامعة القاهرة', N'إعلام', '2005-09-01', '2009-06-30'),

    (N'hany.magdy@bosla.demo', N'جامعة القاهرة', N'هندسة', '2008-09-01', '2012-06-30'),
    (N'hany.magdy@bosla.demo', N'جامعة المنصورة', N'هندسة', '2013-09-01', '2015-06-30'),

    (N'salma.hassan@bosla.demo', N'جامعة القاهرة', N'تربية', '2010-09-01', '2014-06-30'),
    (N'salma.hassan@bosla.demo', N'دار العلوم', N'لغة عربية', '2015-09-01', '2017-06-30'),

    (N'youssef.mahmoud@bosla.demo', N'جامعة القاهرة', N'تجارة', '2007-09-01', '2011-06-30'),
    (N'youssef.mahmoud@bosla.demo', N'جامعة عين شمس', N'تجارة', '2012-09-01', '2014-06-30')
) AS D(Email, Institution, Field, StartDt, EndDt)
INNER JOIN AspNetUsers u ON u.Email = D.Email
WHERE NOT EXISTS (SELECT 1 FROM Educations e
    INNER JOIN AspNetUsers u2 ON u2.Id = e.UserId
    WHERE u2.Email = D.Email AND e.InstitutionName = D.Institution AND e.FieldOfStudy = D.Field);

PRINT N'Inserted education records.';
GO
