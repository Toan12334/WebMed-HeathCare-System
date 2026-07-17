using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class HealthCalculation
{
    public int CalculationId { get; set; }

    public int PatientId { get; set; }

    public string MetricType { get; set; } = null!;

    public string InputData { get; set; } = null!;

    public string CalculatedResult { get; set; } = null!;

    public DateTime CalculatedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
