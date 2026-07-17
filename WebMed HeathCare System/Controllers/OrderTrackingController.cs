using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class OrderTrackingController : Controller
    {
        private readonly WebMedDbContext _context;

        public OrderTrackingController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /OrderTracking
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .Where(o => o.PatientId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /OrderTracking/Track/{id}
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || order.PatientId != userId)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: /OrderTracking/GetLiveStatus/{id}
        [HttpGet]
        public async Task<IActionResult> GetLiveStatus(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || order.PatientId != userId)
            {
                return Forbid();
            }

            // Simulate shipping status changing dynamically or returning delivery parameters
            double lat = 10.762622;
            double lng = 106.660172;
            string eta = "30 mins";

            if (order.OrderStatus == "Shipping")
            {
                // Simulate courier moving dynamically towards destination (cycle every 2 minutes for demo)
                double elapsedSeconds = (DateTime.Now - order.UpdatedAt).TotalSeconds;
                double steps = (elapsedSeconds % 120) / 120.0; // Progression: 0 to 1
                
                lat = 10.762622 + (0.015 * steps);
                lng = 106.660172 + (0.015 * steps);
                eta = $"{(int)((1 - steps) * 15) + 1} mins";
            }

            return Json(new
            {
                status = order.OrderStatus,
                eta = eta,
                latitude = lat,
                longitude = lng,
                updatedAt = order.UpdatedAt.ToString("t")
            });
        }
    }
}
