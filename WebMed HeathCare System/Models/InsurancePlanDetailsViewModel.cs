using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Models
{
    public class InsurancePlanDetailsViewModel
    {
        public InsurancePlan Plan { get; set; } = null!;
        public Coverage? Coverage { get; set; }
        public Pricing? Pricing { get; set; }
        public IEnumerable<Benefit> Benefits { get; set; } = new List<Benefit>();
    }
}
