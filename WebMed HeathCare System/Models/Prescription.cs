using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int ConsultationId { get; set; }

    public int DoctorId { get; set; }

    public int PatientId { get; set; }

    public DateTime IssuedDate { get; set; }

    public string? Notes { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
