using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IPharmacistService
    {
        Task<List<Order>> GetOrdersAsync(string? statusFilter);
        Task<Order?> GetOrderDetailsAsync(int id);
        Task<bool> StartPreparationAsync(int id, int? pharmacistId);
        Task<(bool Success, string? ErrorMessage)> ConfirmPackedAsync(int id);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
        Task<List<Medicine>> GetInventoryAsync();
        Task<bool> RestockAsync(int medicineId, int quantityToAdd);
        Task<bool> AddMedicineAsync(string name, string category, string? description, decimal price, int stockQuantity, bool isPrescriptionRequired);
        Task<bool> UpdateMedicineAsync(int medicineId, string name, string category, string? description, decimal price, int stockQuantity, bool isPrescriptionRequired);
    }
}
