using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class PharmacistService : IPharmacistService
    {
        private readonly WebMedDbContext _context;

        public PharmacistService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersAsync(string? statusFilter)
        {
            var query = _context.Orders
                .Include(o => o.Patient)
                .ThenInclude(p => p.PatientNavigation)
                .Where(o => o.OrderStatus != "Pending" || o.PaymentMethod == "COD")
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.OrderStatus == statusFilter);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<Order?> GetOrderDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Patient)
                .ThenInclude(p => p.PatientNavigation)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<bool> StartPreparationAsync(int id, int? pharmacistId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null || (order.OrderStatus == "Pending" && order.PaymentMethod != "COD"))
            {
                return false;
            }

            if (pharmacistId.HasValue)
            {
                order.PharmacistId = pharmacistId.Value;
            }

            order.OrderStatus = "Preparing";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(bool Success, string? ErrorMessage)> ConfirmPackedAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return (false, "Order not found.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var detail in order.OrderDetails)
                {
                    var med = await _context.Medicines.FindAsync(detail.MedicineId);
                    if (med == null)
                    {
                        return (false, $"Medicine not found for ID: {detail.MedicineId}");
                    }

                    if (med.StockQuantity < detail.Quantity)
                    {
                        return (false, $"Insufficient stock for medicine '{med.Name}'. Available: {med.StockQuantity}, Requested: {detail.Quantity}");
                    }

                    med.StockQuantity -= detail.Quantity;
                }

                order.OrderStatus = "Packed";
                order.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, "An error occurred while confirming order preparation.");
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return false;
            }

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Medicine>> GetInventoryAsync()
        {
            return await _context.Medicines
                .OrderBy(m => m.StockQuantity)
                .ToListAsync();
        }

        public async Task<bool> RestockAsync(int medicineId, int quantityToAdd)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null)
            {
                return false;
            }

            medicine.StockQuantity += quantityToAdd;
            medicine.IsActive = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
