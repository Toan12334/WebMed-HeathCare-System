using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Repositories
{
    public class CoverageRepository : ICoverageRepository
    {
        private readonly WebMedDbContext _context;

        public CoverageRepository(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<Coverage?> GetCoverageDetailsAsync(int planId)
        {
            return await _context.Coverages
                .FirstOrDefaultAsync(c => c.PlanId == planId);
        }
    }
}
