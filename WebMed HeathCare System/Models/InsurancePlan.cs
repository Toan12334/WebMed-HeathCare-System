using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class InsurancePlan
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationMonths { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<PatientInsurance> PatientInsurances { get; set; } = new List<PatientInsurance>();
    
    public virtual Coverage? Coverage { get; set; }
    public virtual Pricing? Pricing { get; set; }
    public virtual ICollection<Benefit> Benefits { get; set; } = new List<Benefit>();
}
