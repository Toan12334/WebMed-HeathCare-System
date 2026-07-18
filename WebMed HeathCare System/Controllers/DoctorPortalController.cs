using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Http;
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

        public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                // check if the user has doctor role
                var isDoctor = User.IsInRole("Doctor");
                if (isDoctor)
                {
                    var actionName = context.ActionDescriptor.RouteValues["action"]?.ToLower();
                    if (actionName != "license" && actionName != "submitlicense" && actionName != "paymentlicense" && actionName != "paylicense")
                    {
                        var doctor = await _context.Doctors.FindAsync(userId);
                        if (doctor != null && !doctor.IsVerified)
                        {
                            TempData["ErrorMessage"] = "You must submit and get your professional license approved by the administrator before accessing other features.";
                            context.Result = RedirectToAction("License");
                            return;
                        }
                    }
                }
            }
            await base.OnActionExecutionAsync(context, next);
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

            var licenses = await _context.DoctorLicenses
                .Where(l => l.DoctorId == userId)
                .OrderByDescending(l => l.SubmittedAt)
                .ToListAsync();

            ViewBag.Licenses = licenses;
            ViewBag.License = licenses.FirstOrDefault(); // Pass latest license as current status context

            return View();
        }

        // POST: /DoctorPortal/License
        [HttpPost]
        public async Task<IActionResult> SubmitLicense(string licenseNumber, decimal feeAmount, IFormFile licenseFile)
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

            if (licenseFile == null || licenseFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Supporting license certificate file is required.";
                return RedirectToAction("License");
            }

            // Check if there is already a pending application (either Pending verification or Pending payment)
            var hasPending = await _context.DoctorLicenses.AnyAsync(l => l.DoctorId == userId && 
                (l.VerificationStatus == "Pending" || l.PaymentStatus == "Pending"));
            if (hasPending)
            {
                TempData["ErrorMessage"] = "You already have a pending license application under review or awaiting payment.";
                return RedirectToAction("License");
            }

            // Process uploaded document file
            string documentUrl = "/uploads/default.pdf";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var fileExtension = Path.GetExtension(licenseFile.FileName);
            var uniqueFileName = "license_" + userId + "_" + DateTime.Now.Ticks + fileExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await licenseFile.CopyToAsync(fileStream);
            }
            documentUrl = "/uploads/" + uniqueFileName;

            var license = new DoctorLicense
            {
                DoctorId = userId,
                LicenseNumber = licenseNumber,
                DocumentUrl = documentUrl,
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
            license.VerificationStatus = "Pending"; // Changes to Pending, waiting for Admin approval!

            // Update doctor verification status (remains false until admin approves)
            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor != null)
            {
                doctor.IsVerified = false;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Verification fee paid successfully! Your licensing documents are now pending administrator approval.";
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
