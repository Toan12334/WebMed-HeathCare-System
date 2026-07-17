using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class PharmacyController : Controller
    {
        private readonly WebMedDbContext _context;

        public PharmacyController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /Pharmacy
        public async Task<IActionResult> Index(string keyword)
        {
            var query = _context.Medicines.Where(m => m.IsActive);
            
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.Name.Contains(keyword) || m.Category.Contains(keyword) || m.Description.Contains(keyword));
            }

            var medicines = await query.ToListAsync();
            ViewBag.Keyword = keyword;
            return View(medicines);
        }

        // GET: /Pharmacy/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == id && m.IsActive);
            if (medicine == null)
            {
                return NotFound();
            }
            return View(medicine);
        }

        // GET: /Pharmacy/Cart
        public IActionResult Cart()
        {
            var cart = GetCartFromSession();
            return View(cart);
        }

        // POST: /Pharmacy/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int medicineId, int quantity)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null || !medicine.IsActive)
            {
                return NotFound();
            }

            // Check inventory stock quantity
            if (medicine.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = "Insufficient stock. Only " + medicine.StockQuantity + " items available.";
                return RedirectToAction("Details", new { id = medicineId });
            }

            var cart = GetCartFromSession();
            var cartItem = cart.FirstOrDefault(c => c.MedicineId == medicineId);

            if (cartItem != null)
            {
                if (medicine.StockQuantity < (cartItem.Quantity + quantity))
                {
                    TempData["ErrorMessage"] = "Cannot add more. Insufficient stock.";
                    return RedirectToAction("Details", new { id = medicineId });
                }
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    MedicineId = medicine.MedicineId,
                    Name = medicine.Name,
                    Price = medicine.Price,
                    Quantity = quantity,
                    IsPrescriptionRequired = medicine.IsPrescriptionRequired
                });
            }

            SaveCartToSession(cart);
            TempData["SuccessMessage"] = medicine.Name + " added to cart successfully.";
            return RedirectToAction("Cart");
        }

        // POST: /Pharmacy/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int medicineId)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.MedicineId == medicineId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
            }
            return RedirectToAction("Cart");
        }

        // POST: /Pharmacy/UpdateCart
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int medicineId, int quantity)
        {
            if (quantity <= 0)
            {
                return RedirectToAction("RemoveFromCart", new { medicineId });
            }

            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null) return NotFound();

            if (medicine.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = $"Only {medicine.StockQuantity} items of {medicine.Name} are available.";
                return RedirectToAction("Cart");
            }

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.MedicineId == medicineId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCartToSession(cart);
            }

            return RedirectToAction("Cart");
        }

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }
    }
}
