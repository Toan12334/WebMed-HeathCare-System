using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class PatientInsurance
{
    public int PatientInsuranceId { get; set; }

    public int PatientId { get; set; }

    public int PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual InsurancePlan Plan { get; set; } = null!;
}
