using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface ICheckoutService
    {
        Task<string?> ValidateCartAsync(List<CartItem> cart);
        Task<(string? Phone, string? Address)> GetPatientContactAsync(int userId);
        Task<(bool Success, string? ErrorMessage, int? OrderId)> PlaceOrderAsync(int userId, List<CartItem> cart, string shippingAddress, string shippingPhone, string? paymentMethod);
    }
}
