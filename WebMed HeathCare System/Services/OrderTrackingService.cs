using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class OrderTrackingService : IOrderTrackingService
    {
        private readonly WebMedDbContext _context;

        public OrderTrackingService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersForPatientAsync(int patientId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .Where(o => o.PatientId == patientId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderForPatientAsync(int orderId, int patientId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.PatientId == patientId);
        }

        public async Task<object?> GetLiveStatusAsync(int orderId, int patientId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.PatientId == patientId);
            if (order == null)
            {
                return null;
            }

            double lat = 10.762622;
            double lng = 106.660172;
            string eta = "30 mins";

            if (order.OrderStatus == "Shipping")
            {
                double elapsedSeconds = (DateTime.Now - order.UpdatedAt).TotalSeconds;
                double steps = (elapsedSeconds % 120) / 120.0;

                lat = 10.762622 + (0.015 * steps);
                lng = 106.660172 + (0.015 * steps);
                eta = $"{(int)((1 - steps) * 15) + 1} mins";
            }

            return new
            {
                status = order.OrderStatus,
                eta,
                latitude = lat,
                longitude = lng,
                updatedAt = order.UpdatedAt.ToString("t")
            };
        }
    }
}
