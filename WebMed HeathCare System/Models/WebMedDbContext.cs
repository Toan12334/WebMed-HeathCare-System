using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebMed_HeathCare_System.Models;

public partial class WebMedDbContext : DbContext
{
    public WebMedDbContext()
    {
    }

    public WebMedDbContext(DbContextOptions<WebMedDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AmbulanceRequest> AmbulanceRequests { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<DoctorLicense> DoctorLicenses { get; set; }

    public virtual DbSet<DoctorReview> DoctorReviews { get; set; }

    public virtual DbSet<HealthCalculation> HealthCalculations { get; set; }

    public virtual DbSet<InsurancePlan> InsurancePlans { get; set; }
    
    public virtual DbSet<Coverage> Coverages { get; set; }
    
    public virtual DbSet<Pricing> Pricings { get; set; }
    
    public virtual DbSet<Benefit> Benefits { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientInsurance> PatientInsurances { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<PrescriptionItem> PrescriptionItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwDoctorReviewAnalytic> VwDoctorReviewAnalytics { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=WebMedDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AmbulanceRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Ambulanc__33A8517A292B6885");

            entity.Property(e => e.AmbulanceLatitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.AmbulanceLongitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.AssignedAmbulanceVehicle)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Eta).HasMaxLength(50);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PickupLocation).HasMaxLength(255);
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Patient).WithMany(p => p.AmbulanceRequests)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ambulance_Patients");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCC2655BAA3A");

            entity.Property(e => e.AppointmentDateTime).HasColumnType("datetime");
            entity.Property(e => e.ConsultationType)
                .HasMaxLength(20)
                .HasDefaultValue("Online");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Scheduled");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Patients");

            entity.HasOne(d => d.Slot).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Slots");
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.ArticleId).HasName("PK__Articles__9C6270E89827A715");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPublished).HasDefaultValue(true);
            entity.Property(e => e.PublishedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Author).WithMany(p => p.Articles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articles_Admins");
        });

        modelBuilder.Entity<AvailabilitySlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__Availabi__0A124AAF0AE11EC9");

            entity.Property(e => e.EndDateTime).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StartDateTime).HasColumnType("datetime");

            entity.HasOne(d => d.Doctor).WithMany(p => p.AvailabilitySlots)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Slots_Doctors");
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.ConsultationId).HasName("PK__Consulta__5D014A98985474D7");

            entity.HasIndex(e => e.AppointmentId, "UQ__Consulta__8ECDFCC3C9F06785").IsUnique();

            entity.Property(e => e.ConductedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithOne(p => p.Consultation)
                .HasForeignKey<Consultation>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Consultations_Appointments");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctors__2DC00EBFB6705AF4");

            entity.HasIndex(e => e.Specialty, "IX_Doctors_Specialty");

            entity.Property(e => e.DoctorId).ValueGeneratedNever();
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Specialty).HasMaxLength(100);

            entity.HasOne(d => d.DoctorNavigation).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctors_Users");
        });

        modelBuilder.Entity<DoctorLicense>(entity =>
        {
            entity.HasKey(e => e.LicenseId).HasName("PK__DoctorLi__72D60082FA4CA1E7");

            entity.Property(e => e.DocumentUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FeeAmount)
                .HasDefaultValue(100000m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Doctor).WithMany(p => p.DoctorLicenses)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoctorLicenses_Doctors");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.DoctorLicenses)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_DoctorLicenses_Admins");
        });

        modelBuilder.Entity<DoctorReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__DoctorRe__74BC79CE0C2FB9C0");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Approved");

            entity.HasOne(d => d.Consultation).WithMany(p => p.DoctorReviews)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Consultations");

            entity.HasOne(d => d.Doctor).WithMany(p => p.DoctorReviews)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.DoctorReviews)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Patients");
        });

        modelBuilder.Entity<HealthCalculation>(entity =>
        {
            entity.HasKey(e => e.CalculationId).HasName("PK__HealthCa__57C05F06D6F8DA1E");

            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CalculatedResult).HasMaxLength(255);
            entity.Property(e => e.InputData).HasMaxLength(500);
            entity.Property(e => e.MetricType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Patient).WithMany(p => p.HealthCalculations)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Calculations_Patients");
        });

        modelBuilder.Entity<InsurancePlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Insuranc__755C22B798E1C8CE");

            entity.Property(e => e.DurationMonths).HasDefaultValue(12);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PlanName).HasMaxLength(100);
        });

        modelBuilder.Entity<Coverage>(entity =>
        {
            entity.HasKey(e => e.CoverageId);
            
            entity.HasIndex(e => e.PlanId).IsUnique();

            entity.HasOne(d => d.Plan).WithOne(p => p.Coverage)
                .HasForeignKey<Coverage>(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Coverages_Plans");
        });

        modelBuilder.Entity<Pricing>(entity =>
        {
            entity.HasKey(e => e.PricingId);
            
            entity.HasIndex(e => e.PlanId).IsUnique();

            entity.Property(e => e.Premium).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Deductible).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Copay).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Plan).WithOne(p => p.Pricing)
                .HasForeignKey<Pricing>(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pricings_Plans");
        });

        modelBuilder.Entity<Benefit>(entity =>
        {
            entity.HasKey(e => e.BenefitId);

            entity.Property(e => e.BenefitName).HasMaxLength(255);

            entity.HasOne(d => d.Plan).WithMany(p => p.Benefits)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Benefits_Plans");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F21289015BBC433");

            entity.HasIndex(e => e.Category, "IX_Medicines_Category");

            entity.HasIndex(e => e.Name, "IX_Medicines_Name");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF4F9F1BEC");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.ShippingAddress).HasMaxLength(255);
            entity.Property(e => e.ShippingPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Patient).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Patients");

            entity.HasOne(d => d.Pharmacist).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PharmacistId)
                .HasConstraintName("FK_Orders_Pharmacists");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D36CB9E9F6E7");

            entity.Property(e => e.PriceAtPurchase).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Medicine).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Details_Medicines");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Details_Orders");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC3665F1716D6");

            entity.Property(e => e.PatientId).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BloodType)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.PatientNavigation).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patients_Users");
        });

        modelBuilder.Entity<PatientInsurance>(entity =>
        {
            entity.HasKey(e => e.PatientInsuranceId).HasName("PK__PatientI__1B84E4367E110D58");

            entity.ToTable("PatientInsurance");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientInsurances)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatInsurance_Patients");

            entity.HasOne(d => d.Plan).WithMany(p => p.PatientInsurances)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatInsurance_Plans");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3808DF71AD");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Completed");
            entity.Property(e => e.PaymentType).HasMaxLength(50);
            entity.Property(e => e.TransactionReference)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__40130832EF616024");

            entity.Property(e => e.IssuedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Consultation).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Consultations");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Patients");
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasKey(e => e.PrescriptionItemId).HasName("PK__Prescrip__1AADD9FA6A240B7B");

            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.DurationDays).HasDefaultValue(7);
            entity.Property(e => e.Frequency).HasMaxLength(100);

            entity.HasOne(d => d.Medicine).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Items_Medicines");

            entity.HasOne(d => d.Prescription).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Items_Prescriptions");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AE9EC5A7B");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61607CBDA206").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C1B765C69");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4B389AFDE").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340AFE3B66").IsUnique();

            entity.Property(e => e.AccountStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<VwDoctorReviewAnalytic>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DoctorReviewAnalytics");

            entity.Property(e => e.CalculatedAverageRating).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
