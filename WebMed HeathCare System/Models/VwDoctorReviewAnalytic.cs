using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class VwDoctorReviewAnalytic
{
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = null!;

    public string Specialty { get; set; } = null!;

    public int? TotalReviews { get; set; }

    public decimal? CalculatedAverageRating { get; set; }

    public int? PositiveReviewsCount { get; set; }

    public int? NegativeReviewsCount { get; set; }
}
