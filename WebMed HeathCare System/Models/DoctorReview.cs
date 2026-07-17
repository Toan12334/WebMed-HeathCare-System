using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class DoctorReview
{
    public int ReviewId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int ConsultationId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string ModerationStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
