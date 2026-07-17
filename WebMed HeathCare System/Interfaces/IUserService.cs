using System.Threading.Tasks;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> RegisterPatientAsync(RegisterViewModel model);
        Task<bool> CheckUserExistsAsync(string username, string email);
    }
}
