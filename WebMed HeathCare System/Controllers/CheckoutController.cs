using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCartFromSession();
            if (cart == null || !cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Add medicines before checking out.";
                return RedirectToAction("Index", "Pharmacy");
            }

            var validationError = await _checkoutService.ValidateCartAsync(cart);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction("Cart", "Pharmacy");
            }

            // Prefill patient phone and address if possible
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                var contact = await _checkoutService.GetPatientContactAsync(userId);
                ViewBag.Phone = contact.Phone;
                ViewBag.Address = contact.Address;
            }

            ViewBag.Cart = cart;
            ViewBag.Total = cart.Sum(i => i.Price * i.Quantity);

            return View();
        }

        // POST: /Checkout
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string shippingPhone, string paymentMethod)
        {
            var cart = GetCartFromSession();
            if (cart == null || !cart.Any())
            {
                TempData["ErrorMessage"] = "Cart is empty.";
                return RedirectToAction("Index", "Pharmacy");
            }

            if (string.IsNullOrWhiteSpace(shippingAddress) || string.IsNullOrWhiteSpace(shippingPhone))
            {
                TempData["ErrorMessage"] = "Shipping address and phone number are required.";
                return RedirectToAction("Index");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var result = await _checkoutService.PlaceOrderAsync(userId, cart, shippingAddress, shippingPhone, paymentMethod);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return result.ErrorMessage != null && result.ErrorMessage.StartsWith("Stock mismatch")
                    ? RedirectToAction("Cart", "Pharmacy")
                    : RedirectToAction("Index");
            }

            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index", "Payment", new { orderId = result.OrderId });
        }

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }
    }
}
