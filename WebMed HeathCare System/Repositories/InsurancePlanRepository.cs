using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Repositories
{
    public class InsurancePlanRepository : IInsurancePlanRepository
    {
        private readonly WebMedDbContext _context;

        public InsurancePlanRepository(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InsurancePlan>> GetAvailablePlansAsync()
        {
            return await _context.InsurancePlans
                .Include(p => p.Pricing)
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<InsurancePlan?> GetPlanByIdAsync(int planId)
        {
            return await _context.InsurancePlans
                .FirstOrDefaultAsync(p => p.PlanId == planId && p.IsActive);
        }

        public async Task<IEnumerable<Benefit>> GetBenefitsAsync(int planId)
        {
            return await _context.Benefits
                .Where(b => b.PlanId == planId)
                .ToListAsync();
        }
    }
}
