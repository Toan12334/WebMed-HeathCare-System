using System.Collections.Generic;
using System.Threading.Tasks;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IInsurancePlanRepository
    {
        Task<IEnumerable<InsurancePlan>> GetAvailablePlansAsync();
        Task<InsurancePlan?> GetPlanByIdAsync(int planId);
        Task<IEnumerable<Benefit>> GetBenefitsAsync(int planId);
    }
}
