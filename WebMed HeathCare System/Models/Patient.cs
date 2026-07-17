using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? BloodType { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AmbulanceRequest> AmbulanceRequests { get; set; } = new List<AmbulanceRequest>();

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<DoctorReview> DoctorReviews { get; set; } = new List<DoctorReview>();

    public virtual ICollection<HealthCalculation> HealthCalculations { get; set; } = new List<HealthCalculation>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PatientInsurance> PatientInsurances { get; set; } = new List<PatientInsurance>();

    public virtual User PatientNavigation { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
