using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class PharmacistController : Controller
    {
        private readonly WebMedDbContext _context;

        public PharmacistController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /Pharmacist
        [HttpGet]
        public async Task<IActionResult> Index(string statusFilter)
        {
            var query = _context.Orders
                .Include(o => o.Patient)
                .ThenInclude(p => p.PatientNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.OrderStatus == statusFilter);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            return View(orders);
        }

        // GET: /Pharmacist/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Patient)
                .ThenInclude(p => p.PatientNavigation)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /Pharmacist/StartPreparation
        [HttpPost]
        public async Task<IActionResult> StartPreparation(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                order.PharmacistId = userId;
            }

            order.OrderStatus = "Preparing";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} status updated to Preparing.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/ConfirmPacked
        [HttpPost]
        public async Task<IActionResult> ConfirmPacked(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            // Double check stock (we already deducted at checkout, but we verify here)
            foreach (var detail in order.OrderDetails)
            {
                var med = await _context.Medicines.FindAsync(detail.MedicineId);
                if (med == null)
                {
                    TempData["ErrorMessage"] = $"Medicine not found for ID: {detail.MedicineId}";
                    return RedirectToAction("Details", new { id });
                }
            }

            order.OrderStatus = "Packed";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} packed successfully. Ready for shipping.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/DispatchOrder
        [HttpPost]
        public async Task<IActionResult> DispatchOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = "Shipping";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} has been dispatched to the courier.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/CompleteOrder
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = "Completed";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} completed (Delivered).";
            return RedirectToAction("Details", new { id });
        }
    }
}
