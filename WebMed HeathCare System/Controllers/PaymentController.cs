using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly WebMedDbContext _context;

        public PaymentController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /Payment?orderId=5
        [HttpGet]
        public async Task<IActionResult> Index(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || order.PatientId != userId)
            {
                return Forbid();
            }

            // If COD, we can auto-process or just show success
            if (order.PaymentMethod == "COD")
            {
                // Create a payment record marked as Pending / COD
                var paymentExists = await _context.Payments.AnyAsync(p => p.AssociatedId == orderId);
                if (!paymentExists)
                {
                    var codPayment = new Payment
                    {
                        UserId = userId,
                        Amount = order.TotalAmount,
                        PaymentType = "COD",
                        PaymentMethod = "COD",
                        PaymentStatus = "Pending",
                        AssociatedId = orderId,
                        PaidAt = DateTime.Now
                    };
                    _context.Payments.Add(codPayment);
                    await _context.SaveChangesAsync();
                }

                return View("Receipt", order);
            }

            return View(order);
        }

        // POST: /Payment/ProcessPayment
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || order.PatientId != userId)
            {
                return Forbid();
            }

            // Simulate Payment Gateway success
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

            TempData["SuccessMessage"] = "Payment processed successfully!";
            return RedirectToAction("Index", new { orderId = order.OrderId });
        }

        // POST: /Payment/FailPayment
        [HttpPost]
        public async Task<IActionResult> FailPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || order.PatientId != userId)
            {
                return Forbid();
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

            ViewBag.Error = "Payment failed. Please choose another method or retry.";
            return View("Index", order);
        }
    }
}
