using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class PharmacistController : Controller
    {
        private readonly IPharmacistService _pharmacistService;

        public PharmacistController(IPharmacistService pharmacistService)
        {
            _pharmacistService = pharmacistService;
        }

        // GET: /Pharmacist
        [HttpGet]
        public async Task<IActionResult> Index(string statusFilter)
        {
            var orders = await _pharmacistService.GetOrdersAsync(statusFilter);

            ViewBag.StatusFilter = statusFilter;
            return View(orders);
        }

        // GET: /Pharmacist/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _pharmacistService.GetOrderDetailsAsync(id);

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
            int? pharmacistId = null;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                pharmacistId = userId;
            }

            var success = await _pharmacistService.StartPreparationAsync(id, pharmacistId);
            if (!success)
            {
                TempData["ErrorMessage"] = "Cannot prepare unpaid online orders.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = $"Order #{id} status updated to Preparing.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/ConfirmPacked
        [HttpPost]
        public async Task<IActionResult> ConfirmPacked(int id)
        {
            var result = await _pharmacistService.ConfirmPackedAsync(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Order #{id} packed successfully. Stock quantities deducted and ready for shipping.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/DispatchOrder
        [HttpPost]
        public async Task<IActionResult> DispatchOrder(int id)
        {
            if (!await _pharmacistService.UpdateOrderStatusAsync(id, "Shipping")) return NotFound();

            TempData["SuccessMessage"] = $"Order #{id} has been dispatched to the courier.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Pharmacist/CompleteOrder
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            if (!await _pharmacistService.UpdateOrderStatusAsync(id, "Completed")) return NotFound();

            TempData["SuccessMessage"] = $"Order #{id} completed (Delivered).";
            return RedirectToAction("Details", new { id });
        }

        // GET: /Pharmacist/Inventory
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var medicines = await _pharmacistService.GetInventoryAsync();

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

            var success = await _pharmacistService.RestockAsync(medicineId, quantityToAdd);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = $"Successfully restocked {quantityToAdd} units.";
            return RedirectToAction("Inventory");
        }
    }
}
