-- =====================================================================================
-- SYSTEM NAME: HEALTH CARE SERVICES SYSTEM (WEBMED)
-- DATABASE SCRIPT FOR MICROSOFT SQL SERVER (REVISED FOR .NET / EF CORE STANDARDS)
-- DESCRIPTION: Full schema mapping of all 31 Use Cases (UC 1 to UC 31)
--              Optimized Naming Conventions, Soft Delete, VND Currencies, 
--              and index systems for High-Performance .NET Applications.
-- =====================================================================================

USE master;
GO

-- 1. DROP DATABASE IF EXISTS & CREATE NEW
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'WebMedDB')
BEGIN
    ALTER DATABASE WebMedDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE WebMedDB;
END
GO

CREATE DATABASE WebMedDB;
GO

USE WebMedDB;
GO

-- =====================================================================================
-- PHASE 1: ACCESS CONTROL, ROLES, AND USER MANAGEMENT (UC 1, 2, 3, 27, 29)
-- =====================================================================================

-- Table for System Roles (UC 29: Manage System Roles & Permissions)
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

-- Main Users Table (UC 1: Register, UC 2: Login, UC 3: Logout, UC 27: Manage User Accounts)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL, 
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PhoneNumber VARCHAR(20) NULL,
    AccountStatus NVARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Suspended, SoftDeleted
    IsActive BIT NOT NULL DEFAULT 1, -- NHÓM 3: Soft Delete Flag thay vì Hard Delete
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- Patient Profiles Extended Data (UC 1: Personal Information)
CREATE TABLE Patients (
    PatientId INT PRIMARY KEY,
    DateOfBirth DATETIME NULL, -- NHÓM 2: Đồng bộ kiểu DATETIME với C#
    Gender NVARCHAR(10) NULL, 
    Address NVARCHAR(255) NULL,
    BloodType VARCHAR(5) NULL,
    IsActive BIT NOT NULL DEFAULT 1, -- NHÓM 3: Soft Delete
    CONSTRAINT FK_Patients_Users FOREIGN KEY (PatientId) REFERENCES Users(UserId) -- NHÓM 3: Bỏ ON DELETE CASCADE
);

-- Doctor Profiles Extended Data (UC 12: Find a Doctor, UC 30: Verify Expert Profiles)
CREATE TABLE Doctors (
    DoctorId INT PRIMARY KEY,
    Specialty NVARCHAR(100) NOT NULL,
    Location NVARCHAR(255) NULL,
    Bio NVARCHAR(MAX) NULL,
    AverageRating DECIMAL(3,2) NOT NULL DEFAULT 0.00,
    IsVerified BIT NOT NULL DEFAULT 0, -- UC 30: Admin Approval Flag
    IsActive BIT NOT NULL DEFAULT 1, -- NHÓM 3: Soft Delete
    CONSTRAINT FK_Doctors_Users FOREIGN KEY (DoctorId) REFERENCES Users(UserId) -- NHÓM 3: Bỏ ON DELETE CASCADE
);

-- NHÓM 3: Chỉ mục tìm kiếm chuyên khoa của Bác sĩ (Tránh Full Table Scan)
CREATE INDEX IX_Doctors_Specialty ON Doctors(Specialty);


-- =====================================================================================
-- PHASE 2: DOCTOR VERIFICATION & LICENSING WORKFLOW (UC 21, 30)
-- =====================================================================================

-- Table to manage Doctor credentials and license fees
CREATE TABLE DoctorLicenses (
    LicenseId INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL,
    LicenseNumber VARCHAR(50) NOT NULL,
    DocumentUrl VARCHAR(500) NOT NULL, -- NHÓM 1: Đổi thành PascalCase và viết thường đuôi Url
    FeeAmount DECIMAL(18,0) NOT NULL DEFAULT 100000, -- NHÓM 3: Chuyển đổi sang DECIMAL(18,0) cho VNĐ
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', 
    VerificationStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', 
    SubmittedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ReviewedBy INT NULL, -- Admin User Id
    ReviewedAt DATETIME NULL,
    CONSTRAINT FK_DoctorLicenses_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_DoctorLicenses_Admins FOREIGN KEY (ReviewedBy) REFERENCES Users(UserId)
);


-- =====================================================================================
-- PHASE 3: CONTENT MANAGEMENT & HEALTH INFORMATION (UC 4, 28)
-- =====================================================================================

-- Table for Health Articles and Medical Info (UC 4: Health A-Z, UC 28: Publish Health News)
CREATE TABLE Articles (
    ArticleId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100) NOT NULL, 
    AuthorId INT NOT NULL, -- Admin User Id
    IsPublished BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1, -- Soft Delete
    PublishedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Articles_Admins FOREIGN KEY (AuthorId) REFERENCES Users(UserId)
);


-- =====================================================================================
-- PHASE 4: WELLNESS TOOLS & CALCULATORS (UC 5)
-- =====================================================================================

-- Logs calculations made by patients (UC 5: Health Calculator)
CREATE TABLE HealthCalculations (
    CalculationId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    MetricType VARCHAR(50) NOT NULL, 
    InputData NVARCHAR(500) NOT NULL, -- JSON string
    CalculatedResult NVARCHAR(255) NOT NULL,
    CalculatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Calculations_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) -- NHÓM 3: Bỏ ON DELETE CASCADE
);


-- =====================================================================================
-- PHASE 5: EMERGENCY AMBULANCE SERVICES (UC 6, 7)
-- =====================================================================================

-- Tracks emergency ambulance requests and real-time monitoring
CREATE TABLE AmbulanceRequests (
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    PickupLocation NVARCHAR(255) NOT NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    EmergencyDetails NVARCHAR(MAX) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- NHÓM 1: Đổi PascalCase
    AssignedAmbulanceVehicle VARCHAR(50) NULL, -- NHÓM 1: Đổi PascalCase
    AmbulanceLatitude DECIMAL(9,6) NULL,  
    AmbulanceLongitude DECIMAL(9,6) NULL, 
    Eta NVARCHAR(50) NULL, -- NHÓM 1: Đổi PascalCase (Eta thay vì ETA)
    RequestedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Ambulance_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId)
);


-- =====================================================================================
-- PHASE 6: INSURANCE PLANS & CORE PAYMENTS (UC 8, 9)
-- =====================================================================================

-- Insurance catalog (UC 8: Insurance Guide)
CREATE TABLE InsurancePlans (
    PlanId INT IDENTITY(1,1) PRIMARY KEY,
    PlanName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DurationMonths INT NOT NULL DEFAULT 12,
    IsActive BIT NOT NULL DEFAULT 1 -- Soft Delete
);

-- Plan Coverages (Separated for CoverageRepository)
CREATE TABLE Coverages (
    CoverageId INT IDENTITY(1,1) PRIMARY KEY,
    PlanId INT NOT NULL UNIQUE,
    CoverageDetails NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_Coverages_Plans FOREIGN KEY (PlanId) REFERENCES InsurancePlans(PlanId)
);

-- Plan Pricings (Separated for PricingRepository)
CREATE TABLE Pricings (
    PricingId INT IDENTITY(1,1) PRIMARY KEY,
    PlanId INT NOT NULL UNIQUE,
    Premium DECIMAL(18,0) NOT NULL,
    Deductible DECIMAL(18,0) NULL,
    Copay DECIMAL(18,0) NULL,
    CONSTRAINT FK_Pricings_Plans FOREIGN KEY (PlanId) REFERENCES InsurancePlans(PlanId)
);

-- Plan Benefits (Separated for InsurancePlanRepository)
CREATE TABLE Benefits (
    BenefitId INT IDENTITY(1,1) PRIMARY KEY,
    PlanId INT NOT NULL,
    BenefitName NVARCHAR(255) NOT NULL,
    CONSTRAINT FK_Benefits_Plans FOREIGN KEY (PlanId) REFERENCES InsurancePlans(PlanId)
);

-- Links patients to purchased plans
CREATE TABLE PatientInsurance (
    PatientInsuranceId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    PlanId INT NOT NULL,
    StartDate DATETIME NOT NULL, -- NHÓM 2: Đồng bộ kiểu DATETIME với C#
    EndDate DATETIME NOT NULL, -- NHÓM 2: Đồng bộ kiểu DATETIME với C#
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active', 
    CONSTRAINT FK_PatInsurance_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_PatInsurance_Plans FOREIGN KEY (PlanId) REFERENCES InsurancePlans(PlanId)
);

-- Master Transaction Ledger (UC 9: Payment Actor Processing Engine)
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, 
    Amount DECIMAL(18,0) NOT NULL, -- NHÓM 3: Chuyển đổi sang DECIMAL(18,0) cho VNĐ
    PaymentType NVARCHAR(50) NOT NULL, 
    PaymentMethod NVARCHAR(50) NOT NULL, 
    TransactionReference VARCHAR(100) NULL, 
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Completed', 
    AssociatedId INT NULL, -- NHÓM 2: Khoá ngoại liên kết động đến OrderId hoặc AppointmentId
    PaidAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payments_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


-- =====================================================================================
-- PHASE 7: DOCTOR SCHEDULING & APPOINTMENTS (UC 13, 14, 22, 23)
-- =====================================================================================

-- Availability configurations set up by doctors (UC 22: Configure Availability Slots)
CREATE TABLE AvailabilitySlots (
    SlotId INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL,
    StartDateTime DATETIME NOT NULL, -- NHÓM 2: Gộp Ngày/Giờ thành DATETIME chuẩn C#
    EndDateTime DATETIME NOT NULL,   -- NHÓM 2: Gộp Ngày/Giờ thành DATETIME chuẩn C#
    IsBooked BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1, -- Soft Delete
    CONSTRAINT FK_Slots_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId) -- NHÓM 3: Bỏ ON DELETE CASCADE
);

-- Appointment Booking System (UC 13: Book Appointment, UC 23: View Consultation Schedule)
CREATE TABLE Appointments (
    AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    SlotId INT NOT NULL,
    AppointmentDateTime DATETIME NOT NULL, -- NHÓM 2: Đổi từ DATE sang DATETIME
    Status NVARCHAR(20) NOT NULL DEFAULT 'Scheduled', -- NHÓM 1: Đổi PascalCase
    ConsultationType NVARCHAR(20) NOT NULL DEFAULT 'Online', 
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_Appointments_Slots FOREIGN KEY (SlotId) REFERENCES AvailabilitySlots(SlotId)
);

-- Clinical Outcomes Data (UC 14: Medical Consultation Details)
CREATE TABLE Consultations (
    ConsultationId INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE,
    Diagnosis NVARCHAR(MAX) NOT NULL,
    TreatmentPlan NVARCHAR(MAX) NULL,
    ConductedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Consultations_Appointments FOREIGN KEY (AppointmentId) REFERENCES Appointments(AppointmentId)
);


-- =====================================================================================
-- PHASE 8: PHARMACY, INVENTORY, AND MEDICAL CATALOGUE (UC 10, 19, 25)
-- =====================================================================================

-- Master Medicine Catalog (UC 10: Shop Medicines Online, UC 25: View Medicine Catalogue)
CREATE TABLE Medicines (
    MedicineId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Category NVARCHAR(100) NOT NULL, 
    Price DECIMAL(18,0) NOT NULL, -- NHÓM 3: Chuyển đổi sang DECIMAL(18,0) cho VNĐ
    StockQuantity INT NOT NULL DEFAULT 0, 
    IsPrescriptionRequired BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1 -- Soft Delete
);

-- NHÓM 3: Index tối ưu hiệu năng tìm kiếm thuốc
CREATE INDEX IX_Medicines_Name ON Medicines(Name);
CREATE INDEX IX_Medicines_Category ON Medicines(Category);


-- =====================================================================================
-- PHASE 9: DIGITAL PRESCRIPTIONS WORKFLOW (UC 24)
-- =====================================================================================

-- Prescriptions linked directly to clinical consultations (UC 24: Issue Digital Prescription)
CREATE TABLE Prescriptions (
    PrescriptionId INT IDENTITY(1,1) PRIMARY KEY,
    ConsultationId INT NOT NULL,
    DoctorId INT NOT NULL,
    PatientId INT NOT NULL,
    IssuedDate DATETIME NOT NULL DEFAULT GETDATE(),
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Prescriptions_Consultations FOREIGN KEY (ConsultationId) REFERENCES Consultations(ConsultationId),
    CONSTRAINT FK_Prescriptions_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId)
);

-- Detail Line Items for Medicines in a Prescription
CREATE TABLE PrescriptionItems (
    PrescriptionItemId INT IDENTITY(1,1) PRIMARY KEY,
    PrescriptionId INT NOT NULL,
    MedicineId INT NOT NULL,
    Dosage NVARCHAR(100) NOT NULL, 
    Frequency NVARCHAR(100) NOT NULL, 
    DurationDays INT NOT NULL DEFAULT 7,
    CONSTRAINT FK_Items_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(PrescriptionId), -- NHÓM 3: Bỏ ON DELETE CASCADE
    CONSTRAINT FK_Items_Medicines FOREIGN KEY (MedicineId) REFERENCES Medicines(MedicineId)
);


-- =====================================================================================
-- PHASE 10: E-PHARMACY ORDERS & FULFILLMENT (UC 11, 16, 17, 18, 20)
-- =====================================================================================

-- E-commerce Orders Management (UC 11: Checkout, UC 17: Manage Orders)
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    TotalAmount DECIMAL(18,0) NOT NULL, -- NHÓM 3: Chuyển đổi sang DECIMAL(18,0) cho VNĐ
    PaymentMethod NVARCHAR(20) NOT NULL, 
    OrderStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', 
    PharmacistId INT NULL, 
    ShippingAddress NVARCHAR(255) NOT NULL, -- NHÓM 2: Bổ sung thông tin địa chỉ giao hàng
    ShippingPhone VARCHAR(20) NOT NULL,     -- NHÓM 2: Bổ sung thông tin SĐT nhận hàng
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Orders_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Orders_Pharmacists FOREIGN KEY (PharmacistId) REFERENCES Users(UserId)
);

-- Line items inside an order
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    MedicineId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PriceAtPurchase DECIMAL(18,0) NOT NULL, -- NHÓM 3: Chuyển đổi sang DECIMAL(18,0) cho VNĐ
    CONSTRAINT FK_Details_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId), -- NHÓM 3: Bỏ ON DELETE CASCADE
    CONSTRAINT FK_Details_Medicines FOREIGN KEY (MedicineId) REFERENCES Medicines(MedicineId)
);


-- =====================================================================================
-- PHASE 11: REVIEWS, RATINGS, AND MODERATION MODALITY (UC 15, 26, 31)
-- =====================================================================================

-- Table for tracking doctor reviews (UC 15: Doctor Review, UC 31: Moderate Patient Reviews)
CREATE TABLE DoctorReviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    ConsultationId INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX) NULL,
    ModerationStatus NVARCHAR(20) NOT NULL DEFAULT 'Approved', 
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reviews_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Reviews_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_Reviews_Consultations FOREIGN KEY (ConsultationId) REFERENCES Consultations(ConsultationId)
);


-- =====================================================================================
-- AUTOMATION & BUSINESS INTELLIGENCE LAYER (UC 26)
-- =====================================================================================

-- NHÓM 3: Trừ kho tự động bằng Trigger đã bị loại bỏ. Toàn bộ nghiệp vụ trừ kho, check âm tồn kho 
--         sẽ được C# xử lý trực tiếp trên tầng Service thông qua DbContext Transaction để đảm bảo
--         việc kiểm soát lỗi và hiển thị thông báo chi tiết ra màn hình UI cho Dược sĩ.

GO
-- VIEW FOR AGGREGATED DOCTOR INSIGHTS (UC 26: Grade View Patient Reviews Aggregate)
CREATE VIEW vw_DoctorReviewAnalytics AS
SELECT 
    d.DoctorId,
    u.FullName AS DoctorName,
    d.Specialty,
    COUNT(r.ReviewId) AS TotalReviews,
    AVG(CAST(r.Rating AS DECIMAL(3,2))) AS CalculatedAverageRating,
    SUM(CASE WHEN r.Rating >= 4 THEN 1 ELSE 0 END) AS PositiveReviewsCount,
    SUM(CASE WHEN r.Rating <= 2 THEN 1 ELSE 0 END) AS NegativeReviewsCount
FROM Doctors d
JOIN Users u ON d.DoctorId = u.UserId
LEFT JOIN DoctorReviews r ON d.DoctorId = r.DoctorId AND r.ModerationStatus = 'Approved'
GROUP BY d.DoctorId, u.FullName, d.Specialty;
GO


-- =====================================================================================
-- SEED DATA LAYER (Ensures structural validity and direct execution readiness)
-- =====================================================================================

-- Populating system roles
INSERT INTO Roles (RoleName, Description) VALUES 
('Patient', 'Standard user who receives treatment, browses info, and orders medicine'),
('Doctor', 'Medical specialist who performs consultations and issues digital prescriptions'),
('Pharmacist', 'Inventory controller who fulfills orders and dispatches shipments'),
('Admin', 'System manager overseeing configurations, compliance, and roles');

-- Seed Admins, Doctors, Pharmacists, and Patients
INSERT INTO Users (RoleId, Username, PasswordHash, FullName, Email, PhoneNumber, AccountStatus) VALUES
(4, 'admin1', 'hash_admin_secure123', 'Nguyen Van Admin', 'admin@webmed.com', '0901234567', 'Active'),
(2, 'dr_smith', 'hash_doc_smith456', 'Dr. John Smith', 'smith@webmed.com', '0912345678', 'Active'),
(3, 'pharm_jane', 'hash_ph_jane789', 'Jane Doe (Pharmacist)', 'jane@webmed.com', '0923456789', 'Active'),
(1, 'patient_alex', 'hash_pat_alex000', 'Alex Rivera', 'alex@gmail.com', '0934567890', 'Active');

-- Add profile extension metrics (VND Format & PascalCase synced)
INSERT INTO Patients (PatientId, DateOfBirth, Gender, Address, BloodType) VALUES
(4, '1995-06-15 00:00:00', 'Male', '123 Medical Boulevard, Hanoi', 'O+');

INSERT INTO Doctors (DoctorId, Specialty, Location, Bio, AverageRating, IsVerified) VALUES
(2, 'Cardiology', 'Block A, Floor 3, WebMed Clinic', 'Expert cardiologist with 15+ years experience.', 5.00, 1);

-- Seed Articles for Health A-Z
INSERT INTO Articles (Title, Content, Category, AuthorId, IsPublished) VALUES
(N'Hiểu về Tăng Huyết Áp', N'Bài viết chi tiết về nguyên nhân, triệu chứng và giải pháp cho bệnh cao huyết áp...', N'Diseases', 1, 1),
(N'Chế độ dinh dưỡng lành mạnh', N'Hướng dẫn tính toán calorie và xây dựng thực đơn tối ưu hàng ngày...', N'Wellness', 1, 1);

-- Seed Medicine Stock Catalogue (VND format)
INSERT INTO Medicines (Name, Description, Category, Price, StockQuantity, IsPrescriptionRequired) VALUES
('Paracetamol 500mg', 'Pain relief and fever reducer', 'Analgesic', 15000, 500, 0),
('Amoxicillin 250mg', 'Broad-spectrum antibiotic', 'Antibiotic', 120000, 200, 1),
('Vitamin C 1000mg', 'Immunity support supplement', 'Vitamin', 85000, 150, 0);

-- Seed Insurance Catalog
INSERT INTO InsurancePlans (PlanName, Description, DurationMonths) VALUES
('WebMed Basic Care', 'Basic plan for regular consultations', 12),
('WebMed Premium Gold', 'Premium plan with comprehensive coverage', 12);

INSERT INTO Coverages (PlanId, CoverageDetails) VALUES
(1, 'Covers up to 50% online consultation costs'),
(2, 'Covers 100% emergency ambulance and 80% medicine bills');

INSERT INTO Pricings (PlanId, Premium, Deductible, Copay) VALUES
(1, 1200000, 200000, 50000),
(2, 4800000, 0, 0);

INSERT INTO Benefits (PlanId, BenefitName) VALUES
(1, 'Free Health Calculator access'),
(1, '5% off pharmacy items'),
(2, 'Priority queueing'),
(2, 'Complete coverage'),
(2, 'Direct billing');

-- Set Doctor Availability Slots (DATETIME format)
INSERT INTO AvailabilitySlots (DoctorId, StartDateTime, EndDateTime, IsBooked) VALUES
(2, DATEADD(hour, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME)), DATEADD(minute, 30, DATEADD(hour, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME))), 0),
(2, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME)), DATEADD(minute, 30, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME))), 0);

-- Populate Doctor Licenses
INSERT INTO DoctorLicenses (DoctorId, LicenseNumber, DocumentUrl, FeeAmount, PaymentStatus, VerificationStatus, SubmittedAt) VALUES 
(2, 'LIC-998877', '/uploads/license_doc.pdf', 100000, 'Completed', 'Approved', GETDATE());

-- Populate Patient Insurance
INSERT INTO PatientInsurance (PatientId, PlanId, StartDate, EndDate, Status) VALUES 
(4, 1, DATEADD(month, -1, GETDATE()), DATEADD(month, 11, GETDATE()), 'Active');

-- Populate Appointments
INSERT INTO Appointments (PatientId, DoctorId, SlotId, AppointmentDateTime, Status, ConsultationType, Notes) VALUES 
(4, 2, 1, DATEADD(hour, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME)), 'Completed', 'Online', 'Patient experiencing mild chest tightness.');

-- Mark slot as booked
UPDATE AvailabilitySlots SET IsBooked = 1 WHERE SlotId = 1;

-- Populate Consultations
INSERT INTO Consultations (AppointmentId, Diagnosis, TreatmentPlan) VALUES 
(1, 'Mild Hypertension detected.', 'Advised low sodium diet and regular blood pressure monitoring. Prescribed Vitamin C and Paracetamol.');

-- Populate Prescriptions
INSERT INTO Prescriptions (ConsultationId, DoctorId, PatientId, Notes) VALUES 
(1, 2, 4, 'Take prescribed medicines after meals.');

-- Populate Prescription Items
INSERT INTO PrescriptionItems (PrescriptionId, MedicineId, Dosage, Frequency, DurationDays) VALUES 
(1, 1, '500mg', 'Twice daily', 5),
(1, 3, '1000mg', 'Once daily', 10);

-- Populate Doctor Reviews
INSERT INTO DoctorReviews (PatientId, DoctorId, ConsultationId, Rating, Comment, ModerationStatus) VALUES 
(4, 2, 1, 5, 'Dr. Smith was extremely professional and thorough.', 'Approved');

-- Populate Orders
INSERT INTO Orders (PatientId, TotalAmount, PaymentMethod, OrderStatus, ShippingAddress, ShippingPhone) VALUES 
(4, 115000, 'COD', 'Completed', '123 Medical Boulevard, Hanoi', '0934567890');

-- Populate Order Details
INSERT INTO OrderDetails (OrderId, MedicineId, Quantity, PriceAtPurchase) VALUES 
(1, 1, 2, 15000),
(1, 3, 1, 85000);

-- Populate Payments Ledger
INSERT INTO Payments (UserId, Amount, PaymentType, PaymentMethod, TransactionReference, PaymentStatus, AssociatedId) VALUES 
(4, 1200000, 'Insurance', 'CreditCard', 'TXN-INS-INIT', 'Completed', 1),
(4, 115000, 'Order', 'COD', 'TXN-ORD-001', 'Completed', 1);

-- Populate Health Calculations
INSERT INTO HealthCalculations (PatientId, MetricType, InputData, CalculatedResult) VALUES 
(4, 'BMI', '{"Weight":70,"Height":175}', '22.86 (Normal)');

-- Populate Ambulance Requests
INSERT INTO AmbulanceRequests (PatientId, PickupLocation, Latitude, Longitude, EmergencyDetails, Status, AssignedAmbulanceVehicle, AmbulanceLatitude, AmbulanceLongitude, Eta) VALUES 
(4, '123 Medical Boulevard, Hanoi', 10.776000, 106.700000, 'High fever and shortness of breath.', 'Completed', 'AMB-1024', 10.776000, 106.700000, '0 mins');

-- Done!
PRINT '=====================================================================================';
PRINT 'SUCCESS: WebMedDB (Clean Architecture & .NET Core optimized) is initialized!';
PRINT '=====================================================================================';