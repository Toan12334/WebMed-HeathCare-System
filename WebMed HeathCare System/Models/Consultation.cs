using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Consultation
{
    public int ConsultationId { get; set; }

    public int AppointmentId { get; set; }

    public string Diagnosis { get; set; } = null!;

    public string? TreatmentPlan { get; set; }

    public DateTime ConductedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual ICollection<DoctorReview> DoctorReviews { get; set; } = new List<DoctorReview>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
