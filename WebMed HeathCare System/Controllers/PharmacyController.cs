using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class PharmacyController : Controller
    {
        private readonly IPharmacyService _pharmacyService;

        public PharmacyController(IPharmacyService pharmacyService)
        {
            _pharmacyService = pharmacyService;
        }

        // GET: /Pharmacy
        public async Task<IActionResult> Index(string keyword)
        {
            var medicines = await _pharmacyService.SearchMedicinesAsync(keyword);
            ViewBag.Keyword = keyword;
            return View(medicines);
        }

        // GET: /Pharmacy/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var medicine = await _pharmacyService.GetActiveMedicineAsync(id);
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
            var medicine = await _pharmacyService.GetMedicineAsync(medicineId);
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

            var medicine = await _pharmacyService.GetMedicineAsync(medicineId);
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
