using System.Threading.Tasks;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface ICoverageRepository
    {
        Task<Coverage?> GetCoverageDetailsAsync(int planId);
    }
}
