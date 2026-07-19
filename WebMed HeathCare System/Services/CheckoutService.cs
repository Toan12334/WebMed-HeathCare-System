using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly WebMedDbContext _context;

        public CheckoutService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<string?> ValidateCartAsync(List<CartItem> cart)
        {
            foreach (var item in cart)
            {
                var med = await _context.Medicines.FindAsync(item.MedicineId);
                if (med == null || !med.IsActive)
                {
                    return $"{item.Name} is no longer available.";
                }

                if (med.StockQuantity < item.Quantity)
                {
                    return $"Sorry, only {med.StockQuantity} of {med.Name} left in stock.";
                }
            }

            return null;
        }

        public async Task<(string? Phone, string? Address)> GetPatientContactAsync(int userId)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientNavigation)
                .FirstOrDefaultAsync(p => p.PatientId == userId);

            return (patient?.PatientNavigation.PhoneNumber, patient?.Address);
        }

        public async Task<(bool Success, string? ErrorMessage, int? OrderId)> PlaceOrderAsync(int userId, List<CartItem> cart, string shippingAddress, string shippingPhone, string? paymentMethod)
        {
            var patient = await _context.Patients.FindAsync(userId);
            if (patient == null)
            {
                return (false, "Only registered patients can place orders.", null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var orderDetails = new List<OrderDetail>();

                foreach (var item in cart)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine == null || !medicine.IsActive || medicine.StockQuantity < item.Quantity)
                    {
                        return (false, $"Stock mismatch for {item.Name}. Order cancelled.", null);
                    }

                    totalAmount += medicine.Price * item.Quantity;
                    orderDetails.Add(new OrderDetail
                    {
                        MedicineId = medicine.MedicineId,
                        Quantity = item.Quantity,
                        PriceAtPurchase = medicine.Price
                    });
                }

                var order = new Order
                {
                    PatientId = userId,
                    TotalAmount = totalAmount,
                    PaymentMethod = paymentMethod ?? "COD",
                    OrderStatus = "Pending",
                    ShippingAddress = shippingAddress,
                    ShippingPhone = shippingPhone,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderDetails = orderDetails
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null, order.OrderId);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, "Error processing your order. Please try again.", null);
            }
        }
    }
}
