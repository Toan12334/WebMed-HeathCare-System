using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class InsuranceGuideController : Controller
    {
        private readonly WebMedDbContext _context;

        public InsuranceGuideController(WebMedDbContext context)
        {
            _context = context;
        }

        // Simulating the InsurancePlanRepository
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _context.InsurancePlans.Where(p => p.IsActive == true).ToListAsync();
            return View(plans);
        }

        // Helper method to attach mock coverage, pricing and benefits to a plan
        private InsurancePlanDetailsViewModel GetEnrichedPlanDetails(InsurancePlan plan)
        {
            var vm = new InsurancePlanDetailsViewModel { Plan = plan };
            
            // Mocking different data based on plan ID or Name
            if (plan.PlanName.ToLower().Contains("premium") || plan.Price > 5000000)
            {
                vm.Coverages = new List<string> { "In-patient Care", "Out-patient Care", "Dental Care", "Maternity", "International Coverage" };
                vm.Benefits = new List<string> { "Private hospital room", "No referral needed for specialists", "Free annual health checkup", "24/7 Telemedicine" };
                vm.Deductible = "0 VND";
                vm.Copay = "10% per visit";
                vm.NetworkType = "PPO (Preferred Provider Organization)";
            }
            else if (plan.PlanName.ToLower().Contains("basic") || plan.Price <= 2000000)
            {
                vm.Coverages = new List<string> { "In-patient Care (Public Hospitals only)", "Accident Emergency" };
                vm.Benefits = new List<string> { "Shared hospital room", "Basic medication coverage" };
                vm.Deductible = "1,000,000 VND/year";
                vm.Copay = "30% per visit";
                vm.NetworkType = "HMO (Health Maintenance Organization)";
            }
            else // Standard / Catch-all
            {
                vm.Coverages = new List<string> { "In-patient Care", "Out-patient Care", "Accident Emergency" };
                vm.Benefits = new List<string> { "Shared hospital room", "Specialist consultation (with referral)", "Basic Dental" };
                vm.Deductible = "500,000 VND/year";
                vm.Copay = "20% per visit";
                vm.NetworkType = "HMO / PPO Hybrid";
            }

            return vm;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _context.InsurancePlans.FindAsync(id);
            if (plan == null) return NotFound();

            // Simulate CoverageRepository, PricingRepository, BenefitsRepository
            var details = GetEnrichedPlanDetails(plan);
            
            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Compare(int plan1, int plan2)
        {
            var p1 = await _context.InsurancePlans.FindAsync(plan1);
            var p2 = await _context.InsurancePlans.FindAsync(plan2);

            if (p1 == null || p2 == null) return NotFound();

            var vm1 = GetEnrichedPlanDetails(p1);
            var vm2 = GetEnrichedPlanDetails(p2);

            ViewBag.Plan1 = vm1;
            ViewBag.Plan2 = vm2;

            return View();
        }
    }
}
