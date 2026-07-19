using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly WebMedDbContext _context;

        public PaymentService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderForPaymentAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task CreateCodPaymentIfNeededAsync(int orderId, int userId, decimal amount)
        {
            var paymentExists = await _context.Payments.AnyAsync(p => p.AssociatedId == orderId && p.PaymentType == "COD");
            if (paymentExists)
            {
                return;
            }

            var codPayment = new Payment
            {
                UserId = userId,
                Amount = amount,
                PaymentType = "COD",
                PaymentMethod = "COD",
                PaymentStatus = "Pending",
                AssociatedId = orderId,
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(codPayment);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, int userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.PatientId != userId)
            {
                return false;
            }

            order.OrderStatus = "Paid";
            order.UpdatedAt = DateTime.Now;

            var payment = new Payment
            {
                UserId = userId,
                Amount = order.TotalAmount,
                PaymentType = "Order",
                PaymentMethod = order.PaymentMethod,
                TransactionReference = "TXN-" + new Random().Next(100000, 999999),
                PaymentStatus = "Completed",
                AssociatedId = orderId,
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> FailPaymentAsync(int orderId, int userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.PatientId != userId)
            {
                return false;
            }

            var payment = new Payment
            {
                UserId = userId,
                Amount = order.TotalAmount,
                PaymentType = "Order",
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = "Failed",
                AssociatedId = orderId,
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
