-- Seeding script for Medical Articles

USE WebMedDB;
GO

-- 1. Clean up existing mock articles
DELETE FROM Articles;

DECLARE @AuthorId INT;
-- Find any Admin or Doctor to be the author
SELECT TOP 1 @AuthorId = UserId FROM Users ORDER BY UserId ASC;

IF @AuthorId IS NULL
BEGIN
    -- If no user exists, create a dummy admin
    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = RoleId FROM Roles WHERE RoleName = 'Admin';
    IF @AdminRoleId IS NULL
    BEGIN
        INSERT INTO Roles (RoleName, Description) VALUES ('Admin', 'Admin role');
        SELECT @AdminRoleId = SCOPE_IDENTITY();
    END
    INSERT INTO Users (RoleId, Username, PasswordHash, FullName, Email, PhoneNumber, AccountStatus, IsActive)
    VALUES (@AdminRoleId, 'admin_system', 'dummy_hash', 'System Administrator', 'admin@webmed.com', '0912345600', 'Active', 1);
    SELECT @AuthorId = SCOPE_IDENTITY();
END

-- Insert mock articles for various categories
INSERT INTO Articles (Title, Content, Category, AuthorId, IsPublished, IsActive, PublishedAt)
VALUES
-- Events
('WebMed Updates Advancements at World Congress 2026', 'The 23rd World Congress on Fetal Medicine (FMF 2026) brought together leading international experts to share the latest research, diagnostic methodologies, and therapeutic protocols. WebMed presented a comprehensive update on maternal-fetal medicine.', 'Events', @AuthorId, 1, 1, GETDATE()),
('Clinical Pharmacy Workshop at WebMed University Hospital', 'A practical seminar detailing the latest workflows, safety guidelines, and clinical drug interaction audits in hospital pharmacies. Experts shared evidence-based guidelines on drug safety monitoring.', 'Events', @AuthorId, 1, 1, DATEADD(day, -5, GETDATE())),
('International Fetal Health & Prenatal Diagnostics Symposium 2026', 'A major conference gathering global specialists to discuss advanced genetic testing, high-resolution ultrasound, and early intervention treatments for prenatal conditions.', 'Events', @AuthorId, 1, 1, DATEADD(day, -8, GETDATE())),
('Orthopedic and Maxillofacial Reconstructive Surgery Seminar', 'A focused workshop demonstrating advanced surgical methodologies, computerized modeling, and post-operative physical therapy for patients recovering from complex reconstructive surgeries.', 'Events', @AuthorId, 1, 1, DATEADD(day, -10, GETDATE())),

-- News
('Breakthrough in Minimally Invasive Robotic Surgery', 'WebMed has successfully integrated the latest robotic surgery systems, enabling highly precise procedures with shorter recovery times and minimal scarring for patients.', 'News', @AuthorId, 1, 1, DATEADD(day, -2, GETDATE())),
('New State-of-the-Art Cardiology Wing Inaugurated', 'Our hospital has opened a new dedicated cardiology care department, equipped with advanced hybrid operating rooms and 24/7 cardiac monitoring facilities.', 'News', @AuthorId, 1, 1, DATEADD(day, -6, GETDATE())),

-- Notifications
('Hospital Holiday Service Hours and Emergency Guidelines', 'Please find the schedule of general clinic operations and specialist availability during the upcoming national holiday. Emergency services remain fully active 24/7.', 'Notifications', @AuthorId, 1, 1, DATEADD(day, -1, GETDATE())),

-- Hospital Activities
('Annual Patient Care Quality Review and Standards Audit', 'WebMed conducted its annual comprehensive quality audit. The hospital maintained high ratings across hygiene, patient satisfaction, and treatment success rates.', 'Hospital Activities', @AuthorId, 1, 1, DATEADD(day, -12, GETDATE())),

-- Scientific Research
('New Study on Maternal Genetic Screening Efficacy Published', 'Our clinical research division has published a peer-reviewed paper in the International Journal of Prenatal Health detailing the diagnostic accuracy of non-invasive genetic screens.', 'Research', @AuthorId, 1, 1, DATEADD(day, -15, GETDATE())),

-- Cooperation
('Global Partnership Signed with Stockholm Medical Institute', 'WebMed is proud to announce a collaborative partnership focusing on specialist exchange programs and joint clinical trials for advanced therapies.', 'Cooperation', @AuthorId, 1, 1, DATEADD(day, -20, GETDATE())),

-- Training
('Advanced Neonatal Life Support Certification Program', 'Our senior pediatric and obstetric staff completed an intensive training program on neonatal emergency resuscitation techniques, ensuring world-class infant care standards.', 'Training', @AuthorId, 1, 1, DATEADD(day, -4, GETDATE())),

-- Community
('Free Medical Checkup Campaign for Rural Communes 2026', 'WebMed organized a voluntary outreach mission, providing free medical examinations, pediatric health counseling, and essential medicines to over 500 families.', 'Community', @AuthorId, 1, 1, DATEADD(day, -14, GETDATE())),

-- Pharma Info
('Essential Safety Updates on Anti-hypertensive Medications', 'The Clinical Pharmacy Department has released a safety bulletin detailing dosage optimization and key monitoring parameters for patients using beta-blocker combinations.', 'Pharma Info', @AuthorId, 1, 1, DATEADD(day, -7, GETDATE())),

-- Recruitment
('Hiring Senior Pediatricians & Cardiology Specialists', 'Join our world-class medical team. WebMed is seeking board-certified specialists to lead our expanding outpatient and clinical research departments.', 'Recruitment', @AuthorId, 1, 1, DATEADD(day, -3, GETDATE())),

-- Success Stories
('Miraculous Recovery: 32-Week Premature Infant Discharged', 'Thanks to the persistent dedication of our Neonatal Intensive Care Unit (NICU), a premature baby born with severe pulmonary distress has been discharged in perfect health.', 'Success Stories', @AuthorId, 1, 1, DATEADD(day, -18, GETDATE()));

PRINT 'Medical articles seeded successfully!';
GO
