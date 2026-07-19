using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class OrderTrackingController : Controller
    {
        private readonly IOrderTrackingService _orderTrackingService;

        public OrderTrackingController(IOrderTrackingService orderTrackingService)
        {
            _orderTrackingService = orderTrackingService;
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

            var orders = await _orderTrackingService.GetOrdersForPatientAsync(userId);

            return View(orders);
        }

        // GET: /OrderTracking/Track/{id}
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Forbid();
            }

            var order = await _orderTrackingService.GetOrderForPatientAsync(id, userId);
            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /OrderTracking/GetLiveStatus/{id}
        [HttpGet]
        public async Task<IActionResult> GetLiveStatus(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Forbid();
            }

            var status = await _orderTrackingService.GetLiveStatusAsync(id, userId);
            if (status == null) return NotFound();

            return Json(status);
        }
    }
}
