using WebMed_HeathCare_System.Models;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models
{
    public class InsurancePlanCompareViewModel
    {
        public InsurancePlanDetailsViewModel Plan1 { get; set; } = null!;
        public InsurancePlanDetailsViewModel Plan2 { get; set; } = null!;
    }
}
