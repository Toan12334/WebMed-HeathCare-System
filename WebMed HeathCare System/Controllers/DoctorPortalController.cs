using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class DoctorPortalController : Controller
    {
        private readonly WebMedDbContext _context;

        public DoctorPortalController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /DoctorPortal/License
        [HttpGet]
        public async Task<IActionResult> License()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            // Verify if doctor
            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Only doctors can access the portal.";
                return RedirectToAction("Index", "Home");
            }

            var license = await _context.DoctorLicenses.FirstOrDefaultAsync(l => l.DoctorId == userId);
            ViewBag.License = license;

            return View();
        }

        // POST: /DoctorPortal/License
        [HttpPost]
        public async Task<IActionResult> SubmitLicense(string licenseNumber, decimal feeAmount)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor == null) return Forbid();

            if (string.IsNullOrWhiteSpace(licenseNumber))
            {
                TempData["ErrorMessage"] = "License number is required.";
                return RedirectToAction("License");
            }

            var license = new DoctorLicense
            {
                DoctorId = userId,
                LicenseNumber = licenseNumber,
                DocumentUrl = "/uploads/license_" + userId + ".pdf", // Mock document URL
                FeeAmount = feeAmount > 0 ? feeAmount : 150000m,
                PaymentStatus = "Pending",
                VerificationStatus = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.DoctorLicenses.Add(license);
            await _context.SaveChangesAsync();

            return RedirectToAction("PaymentLicense", new { licenseId = license.LicenseId });
        }

        // GET: /DoctorPortal/PaymentLicense/{licenseId}
        [HttpGet]
        public async Task<IActionResult> PaymentLicense(int licenseId)
        {
            var license = await _context.DoctorLicenses.FindAsync(licenseId);
            if (license == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || license.DoctorId != userId)
            {
                return Forbid();
            }

            return View(license);
        }

        // POST: /DoctorPortal/PayLicense
        [HttpPost]
        public async Task<IActionResult> PayLicense(int licenseId)
        {
            var license = await _context.DoctorLicenses.FindAsync(licenseId);
            if (license == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || license.DoctorId != userId)
            {
                return Forbid();
            }

            // Simulate success
            license.PaymentStatus = "Paid";
            license.VerificationStatus = "Approved"; // Auto-approve for testing flow speed!

            // Update doctor verification status
            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor != null)
            {
                doctor.IsVerified = true;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Verification fee paid successfully! Your account is now verified.";
            return RedirectToAction("License");
        }

        // GET: /DoctorPortal/Availability
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            var slots = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == userId && s.IsActive)
                .OrderBy(s => s.StartDateTime)
                .ToListAsync();

            return View(slots);
        }

        // POST: /DoctorPortal/AddSlot
        [HttpPost]
        public async Task<IActionResult> AddSlot(DateTime startDateTime, DateTime endDateTime)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            if (startDateTime >= endDateTime)
            {
                TempData["ErrorMessage"] = "Start time must be before end time.";
                return RedirectToAction("Availability");
            }

            if (startDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Time slot cannot be in the past.";
                return RedirectToAction("Availability");
            }

            // Check conflicts with existing slots
            var conflict = await _context.AvailabilitySlots
                .AnyAsync(s => s.DoctorId == userId && s.IsActive &&
                               ((startDateTime >= s.StartDateTime && startDateTime < s.EndDateTime) ||
                                (endDateTime > s.StartDateTime && endDateTime <= s.EndDateTime) ||
                                (startDateTime <= s.StartDateTime && endDateTime >= s.EndDateTime)));

            if (conflict)
            {
                TempData["ErrorMessage"] = "This time slot conflicts with an existing slot.";
                return RedirectToAction("Availability");
            }

            var slot = new AvailabilitySlot
            {
                DoctorId = userId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                IsBooked = false,
                IsActive = true
            };

            _context.AvailabilitySlots.Add(slot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Availability slot added successfully.";
            return RedirectToAction("Availability");
        }

        // POST: /DoctorPortal/DeleteSlot
        [HttpPost]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            var slot = await _context.AvailabilitySlots.FindAsync(slotId);
            if (slot == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || slot.DoctorId != userId)
            {
                return Forbid();
            }

            if (slot.IsBooked)
            {
                TempData["ErrorMessage"] = "Cannot delete slot because an appointment has already been booked.";
                return RedirectToAction("Availability");
            }

            slot.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Slot removed successfully.";
            return RedirectToAction("Availability");
        }

        // GET: /DoctorPortal/Reviews
        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor == null) return Forbid();

            var reviews = await _context.DoctorReviews
                .Include(r => r.Patient).ThenInclude(p => p.PatientNavigation)
                .Where(r => r.DoctorId == userId && r.ModerationStatus == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Doctor = doctor;
            return View(reviews);
        }
    }
}
