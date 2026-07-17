using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class DoctorLicense
{
    public int LicenseId { get; set; }

    public int DoctorId { get; set; }

    public string LicenseNumber { get; set; } = null!;

    public string DocumentUrl { get; set; } = null!;

    public decimal FeeAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string VerificationStatus { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
