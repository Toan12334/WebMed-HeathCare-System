using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Pricing
{
    public int PricingId { get; set; }

    public int PlanId { get; set; }

    public decimal Premium { get; set; }

    public decimal? Deductible { get; set; }

    public decimal? Copay { get; set; }

    public virtual InsurancePlan Plan { get; set; } = null!;
}
