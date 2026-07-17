using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Coverage
{
    public int CoverageId { get; set; }

    public int PlanId { get; set; }

    public string CoverageDetails { get; set; } = null!;

    public virtual InsurancePlan Plan { get; set; } = null!;
}
