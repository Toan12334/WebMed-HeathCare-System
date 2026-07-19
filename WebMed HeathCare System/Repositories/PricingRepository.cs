using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Repositories
{
    public class PricingRepository : IPricingRepository
    {
        private readonly WebMedDbContext _context;

        public PricingRepository(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<Pricing?> GetPricingInformationAsync(int planId)
        {
            return await _context.Pricings
                .FirstOrDefaultAsync(p => p.PlanId == planId);
        }
    }
}
