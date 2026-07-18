-- Seeding script for English Doctor Accounts (Clean English, No Encoding Issues)

USE WebMedDB;
GO

-- 1. Clean up any existing seeded doctors to prevent duplicates and remove corrupted encoding records
DECLARE @DocList TABLE (UserId INT);
INSERT INTO @DocList (UserId)
SELECT UserId FROM Users WHERE Username LIKE 'dr_%';

DELETE FROM PrescriptionItems WHERE PrescriptionId IN (
    SELECT PrescriptionId FROM Prescriptions WHERE ConsultationId IN (
        SELECT ConsultationId FROM Consultations WHERE AppointmentId IN (
            SELECT AppointmentId FROM Appointments WHERE DoctorId IN (SELECT UserId FROM @DocList)
        )
    )
);

DELETE FROM Prescriptions WHERE ConsultationId IN (
    SELECT ConsultationId FROM Consultations WHERE AppointmentId IN (
        SELECT AppointmentId FROM Appointments WHERE DoctorId IN (SELECT UserId FROM @DocList)
    )
);

DELETE FROM DoctorReviews WHERE DoctorId IN (SELECT UserId FROM @DocList);

DELETE FROM Consultations WHERE AppointmentId IN (
    SELECT AppointmentId FROM Appointments WHERE DoctorId IN (SELECT UserId FROM @DocList)
);

DELETE FROM Appointments WHERE DoctorId IN (SELECT UserId FROM @DocList);
DELETE FROM AvailabilitySlots WHERE DoctorId IN (SELECT UserId FROM @DocList);
DELETE FROM DoctorLicenses WHERE DoctorId IN (SELECT UserId FROM @DocList);
DELETE FROM Doctors WHERE DoctorId IN (SELECT UserId FROM @DocList);
DELETE FROM Users WHERE Username LIKE 'dr_%';

DECLARE @DoctorRoleId INT;
SELECT @DoctorRoleId = RoleId FROM Roles WHERE RoleName = 'Doctor';

-- If Doctor role does not exist, insert it
IF @DoctorRoleId IS NULL
BEGIN
    INSERT INTO Roles (RoleName, Description) VALUES ('Doctor', 'Doctor role');
    SELECT @DoctorRoleId = SCOPE_IDENTITY();
END

-- Create a helper temp table for Doctors to seed
IF OBJECT_ID('tempdb..#DoctorSeed') IS NOT NULL DROP TABLE #DoctorSeed;
CREATE TABLE #DoctorSeed (
    Username VARCHAR(50),
    FullName NVARCHAR(100),
    Email VARCHAR(100),
    PhoneNumber VARCHAR(20),
    Specialty NVARCHAR(100),
    Location NVARCHAR(255),
    Bio NVARCHAR(MAX)
);

-- Using strictly English strings for Specialties and Locations to ensure no character encoding issues
INSERT INTO #DoctorSeed (Username, FullName, Email, PhoneNumber, Specialty, Location, Bio)
VALUES
('dr_arthur', 'Assoc. Prof. Dr. Arthur Pendelton', 'arthur.pendelton@webmed.com', '0912345671', 'Fetal Medicine Center', 'Hanoi Clinic', 'Assoc. Prof. Arthur Pendelton is a leading expert in fetal medicine with over 20 years of clinical experience.'),
('dr_elizabeth', 'Prof. Dr. Elizabeth Holmes', 'elizabeth.holmes@webmed.com', '0912345672', 'Fetal Medicine Center', 'Hanoi Clinic', 'Prof. Elizabeth Holmes focuses on maternal-fetal medical research and advanced prenatal diagnostics.'),
('dr_david', 'Dr. David Miller, PhD', 'david.miller@webmed.com', '0912345673', 'Oncology Center', 'HCMC Clinic', 'Dr. David Miller is a board-certified oncologist specializing in immunotherapy and clinical cancer trials.'),
('dr_sarah', 'Prof. Dr. Sarah Jenkins', 'sarah.jenkins@webmed.com', '0912345674', 'Cardiology Center', 'Hanoi Clinic', 'Prof. Sarah Jenkins is an interventional cardiologist committed to state-of-the-art cardiovascular health care.'),
('dr_james', 'Dr. James Carter, MD', 'james.carter@webmed.com', '0912345675', 'Obstetrics Department', 'HCMC Clinic', 'Dr. James Carter is an obstetrician-gynecologist focused on high-risk pregnancy management and general gynecology.'),
('dr_emily', 'Dr. Emily Watson', 'emily.watson@webmed.com', '0912345676', 'Gynecology Department', 'Hanoi Clinic', 'Dr. Emily Watson provides comprehensive gynecological services and minimally invasive surgeries.'),
('dr_robert', 'Assoc. Prof. Dr. Robert Taylor', 'robert.taylor@webmed.com', '0912345677', 'Pediatrics Department', 'HCMC Clinic', 'Assoc. Prof. Robert Taylor is a dedicated pediatrician specializing in childhood growth and developmental disorders.'),
('dr_linda', 'Dr. Linda Ross, PhD', 'linda.ross@webmed.com', '0912345678', 'Gastroenterology Center', 'Hanoi Clinic', 'Dr. Linda Ross is a gastroenterologist with expertise in endoscopy and inflammatory bowel diseases.'),
('dr_william', 'Dr. William Vance, MD', 'william.vance@webmed.com', '0912345679', 'Traditional Medicine Center', 'HCMC Clinic', 'Dr. William Vance integrates traditional Eastern medicine practices with modern Western clinical diagnostics.');

-- Cursor to insert records
DECLARE @Username VARCHAR(50), @FullName NVARCHAR(100), @Email VARCHAR(100), @PhoneNumber VARCHAR(20), @Specialty NVARCHAR(100), @Location NVARCHAR(255), @Bio NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR 
SELECT Username, FullName, Email, PhoneNumber, Specialty, Location, Bio FROM #DoctorSeed;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @Username, @FullName, @Email, @PhoneNumber, @Specialty, @Location, @Bio;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Double check user doesn't exist
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
    BEGIN
        -- Insert into Users
        INSERT INTO Users (RoleId, Username, PasswordHash, FullName, Email, PhoneNumber, AccountStatus, IsActive, CreatedAt, UpdatedAt)
        VALUES (@DoctorRoleId, @Username, '$2a$11$e0MYz.tpy2Q1234567890.abcdefghijklmnopqrstuvwxyz', @FullName, @Email, @PhoneNumber, 'Active', 1, GETDATE(), GETDATE());
        
        DECLARE @NewUserId INT = SCOPE_IDENTITY();
        
        -- Insert into Doctors
        INSERT INTO Doctors (DoctorId, Specialty, Location, Bio, AverageRating, IsVerified, IsActive)
        VALUES (@NewUserId, @Specialty, @Location, @Bio, 4.80, 1, 1);

        -- Insert a license
        INSERT INTO DoctorLicenses (DoctorId, LicenseNumber, DocumentUrl, FeeAmount, PaymentStatus, VerificationStatus, SubmittedAt)
        VALUES (@NewUserId, 'LIC-' + UPPER(@Username), 'http://example.com/docs/license.pdf', 150000, 'Approved', 'Approved', GETDATE());
        
        -- Create availability slots
        INSERT INTO AvailabilitySlots (DoctorId, StartDateTime, EndDateTime, IsBooked, IsActive)
        VALUES 
        (@NewUserId, DATEADD(hour, 9, CAST(CAST(DATEADD(day, 1, GETDATE()) AS DATE) AS DATETIME)), DATEADD(hour, 10, CAST(CAST(DATEADD(day, 1, GETDATE()) AS DATE) AS DATETIME)), 0, 1),
        (@NewUserId, DATEADD(hour, 14, CAST(CAST(DATEADD(day, 1, GETDATE()) AS DATE) AS DATETIME)), DATEADD(hour, 15, CAST(CAST(DATEADD(day, 1, GETDATE()) AS DATE) AS DATETIME)), 0, 1),
        (@NewUserId, DATEADD(hour, 10, CAST(CAST(DATEADD(day, 2, GETDATE()) AS DATE) AS DATETIME)), DATEADD(hour, 11, CAST(CAST(DATEADD(day, 2, GETDATE()) AS DATE) AS DATETIME)), 0, 1);
    END
    FETCH NEXT FROM db_cursor INTO @Username, @FullName, @Email, @PhoneNumber, @Specialty, @Location, @Bio;
END;

CLOSE db_cursor;
DEALLOCATE db_cursor;

DROP TABLE #DoctorSeed;
PRINT 'Clean English doctor seeding completed successfully!';
GO
