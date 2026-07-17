using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Models
{
    public class InsurancePlanDetailsViewModel
    {
        public InsurancePlan Plan { get; set; } = null!;
        
        // Extended properties to mock Coverage, Pricing, and Benefits Repository
        public List<string> Coverages { get; set; } = new List<string>();
        public List<string> Benefits { get; set; } = new List<string>();
        public string Deductible { get; set; } = string.Empty;
        public string Copay { get; set; } = string.Empty;
        public string NetworkType { get; set; } = string.Empty;
    }
}
