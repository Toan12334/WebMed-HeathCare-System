using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string Specialty { get; set; } = null!;

    public string? Location { get; set; }

    public string? Bio { get; set; }

    public decimal AverageRating { get; set; }

    public bool IsVerified { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; set; } = new List<AvailabilitySlot>();

    public virtual ICollection<DoctorLicense> DoctorLicenses { get; set; } = new List<DoctorLicense>();

    public virtual User DoctorNavigation { get; set; } = null!;

    public virtual ICollection<DoctorReview> DoctorReviews { get; set; } = new List<DoctorReview>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
