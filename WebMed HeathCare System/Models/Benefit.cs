using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Benefit
{
    public int BenefitId { get; set; }

    public int PlanId { get; set; }

    public string BenefitName { get; set; } = null!;

    public virtual InsurancePlan Plan { get; set; } = null!;
}
