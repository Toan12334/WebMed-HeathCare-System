using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IOrderTrackingService
    {
        Task<List<Order>> GetOrdersForPatientAsync(int patientId);
        Task<Order?> GetOrderForPatientAsync(int orderId, int patientId);
        Task<object?> GetLiveStatusAsync(int orderId, int patientId);
    }
}
