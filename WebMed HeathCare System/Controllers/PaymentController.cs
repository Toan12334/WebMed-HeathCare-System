using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: /Payment?orderId=5
        [HttpGet]
        public async Task<IActionResult> Index(int orderId)
        {
            var order = await _paymentService.GetOrderForPaymentAsync(orderId);

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
                await _paymentService.CreateCodPaymentIfNeededAsync(orderId, userId, order.TotalAmount);

                return View("Receipt", order);
            }

            return View(order);
        }

        // POST: /Payment/ProcessPayment
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Forbid();
            }

            var success = await _paymentService.ProcessPaymentAsync(orderId, userId);
            if (!success)
            {
                return Forbid();
            }

            TempData["SuccessMessage"] = "Payment processed successfully!";
            return RedirectToAction("Index", new { orderId });
        }

        // POST: /Payment/FailPayment
        [HttpPost]
        public async Task<IActionResult> FailPayment(int orderId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Forbid();
            }

            var success = await _paymentService.FailPaymentAsync(orderId, userId);
            if (!success)
            {
                return Forbid();
            }

            var order = await _paymentService.GetOrderForPaymentAsync(orderId);
            if (order == null) return NotFound();
            ViewBag.Error = "Payment failed. Please choose another method or retry.";
            return View("Index", order);
        }
    }
}
