using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly WebMedDbContext _context;

        public CheckoutController(WebMedDbContext context)
        {
            _context = context;
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

            // Verify stock and details from DB
            foreach (var item in cart)
            {
                var med = await _context.Medicines.FindAsync(item.MedicineId);
                if (med == null || !med.IsActive)
                {
                    TempData["ErrorMessage"] = $"{item.Name} is no longer available.";
                    return RedirectToAction("Cart", "Pharmacy");
                }
                if (med.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sorry, only {med.StockQuantity} of {med.Name} left in stock.";
                    return RedirectToAction("Cart", "Pharmacy");
                }
            }

            // Prefill patient phone and address if possible
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                var patient = await _context.Patients
                    .Include(p => p.PatientNavigation)
                    .FirstOrDefaultAsync(p => p.PatientId == userId);
                if (patient != null)
                {
                    ViewBag.Phone = patient.PatientNavigation.PhoneNumber;
                    ViewBag.Address = patient.Address;
                }
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

            var patient = await _context.Patients.FindAsync(userId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Only registered patients can place orders.";
                return RedirectToAction("Index");
            }

            // Verify stock under transaction
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    decimal totalAmount = 0;
                    var orderDetails = new List<OrderDetail>();

                    foreach (var item in cart)
                    {
                        var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                        if (medicine == null || !medicine.IsActive || medicine.StockQuantity < item.Quantity)
                        {
                            TempData["ErrorMessage"] = $"Stock mismatch for {item.Name}. Order cancelled.";
                            return RedirectToAction("Cart", "Pharmacy");
                        }

                        // Verification successful, total amount calculated (Stock deduction moved to Pharmacist packing confirmation)
                        totalAmount += medicine.Price * item.Quantity;

                        orderDetails.Add(new OrderDetail
                        {
                            MedicineId = medicine.MedicineId,
                            Quantity = item.Quantity,
                            PriceAtPurchase = medicine.Price
                        });
                    }

                    var order = new Order
                    {
                        PatientId = userId,
                        TotalAmount = totalAmount,
                        PaymentMethod = paymentMethod ?? "COD",
                        OrderStatus = "Pending",
                        ShippingAddress = shippingAddress,
                        ShippingPhone = shippingPhone,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        OrderDetails = orderDetails
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Clear cart
                    HttpContext.Session.Remove("Cart");

                    return RedirectToAction("Index", "Payment", new { orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Error processing your order. Please try again.";
                    return RedirectToAction("Cart", "Pharmacy");
                }
            }
        }

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }
    }
}
