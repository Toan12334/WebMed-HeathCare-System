using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IPaymentService
    {
        Task<Order?> GetOrderForPaymentAsync(int orderId);
        Task CreateCodPaymentIfNeededAsync(int orderId, int userId, decimal amount);
        Task<bool> ProcessPaymentAsync(int orderId, int userId);
        Task<bool> FailPaymentAsync(int orderId, int userId);
    }
}
