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
                .Where(o => o.OrderStatus != "Pending" || o.PaymentMethod == "COD")
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

            // Backend verification: Prevent pharmacist from viewing unpaid online orders
            if (order.OrderStatus == "Pending" && order.PaymentMethod != "COD")
            {
                TempData["ErrorMessage"] = "Cannot view or prepare unpaid online orders.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // POST: /Pharmacist/StartPreparation
        [HttpPost]
        public async Task<IActionResult> StartPreparation(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Backend verification: Prevent pharmacist from preparing unpaid online orders
            if (order.OrderStatus == "Pending" && order.PaymentMethod != "COD")
            {
                TempData["ErrorMessage"] = "Cannot prepare unpaid online orders.";
                return RedirectToAction("Index");
            }

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

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Verify and deduct stock quantity
                    foreach (var detail in order.OrderDetails)
                    {
                        var med = await _context.Medicines.FindAsync(detail.MedicineId);
                        if (med == null)
                        {
                            TempData["ErrorMessage"] = $"Medicine not found for ID: {detail.MedicineId}";
                            return RedirectToAction("Details", new { id });
                        }

                        if (med.StockQuantity < detail.Quantity)
                        {
                            TempData["ErrorMessage"] = $"Insufficient stock for medicine '{med.Name}'. Available: {med.StockQuantity}, Requested: {detail.Quantity}";
                            return RedirectToAction("Details", new { id });
                        }

                        // Deduct stock quantity here to satisfy UC 19
                        med.StockQuantity -= detail.Quantity;
                    }

                    order.OrderStatus = "Packed";
                    order.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Order #{id} packed successfully. Stock quantities deducted and ready for shipping.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while confirming order preparation.";
                }
            }

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

        // GET: /Pharmacist/Inventory
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var medicines = await _context.Medicines
                .OrderBy(m => m.StockQuantity)
                .ToListAsync();

            return View(medicines);
        }

        // POST: /Pharmacist/Restock
        [HttpPost]
        public async Task<IActionResult> Restock(int medicineId, int quantityToAdd)
        {
            if (quantityToAdd <= 0)
            {
                TempData["ErrorMessage"] = "Quantity to add must be greater than 0.";
                return RedirectToAction("Inventory");
            }

            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null) return NotFound();

            medicine.StockQuantity += quantityToAdd;
            medicine.IsActive = true; // reactivate if it was deactivated due to out of stock
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully restocked {quantityToAdd} units of '{medicine.Name}'. Total stock is now {medicine.StockQuantity}.";
            return RedirectToAction("Inventory");
        }
    }
}
