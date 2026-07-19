using System.Threading.Tasks;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IEmergencyRequestService
    {
        Task<AmbulanceRequest> CreateRequestAsync(int? patientId, string pickupLocation, string emergencyDetails, decimal? latitude, decimal? longitude);
        Task<AmbulanceRequest?> GetRequestByIdAsync(int requestId);
        Task<object?> GetTrackingDataAsync(int requestId);
    }
}
