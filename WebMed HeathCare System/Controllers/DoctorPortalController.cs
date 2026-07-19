using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Http;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class DoctorPortalController : Controller
    {
        private readonly IDoctorPortalService _doctorPortalService;

        public DoctorPortalController(IDoctorPortalService doctorPortalService)
        {
            _doctorPortalService = doctorPortalService;
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
                        if (await _doctorPortalService.IsUnverifiedDoctorAsync(userId))
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
            var doctor = await _doctorPortalService.GetDoctorAsync(userId);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Only doctors can access the portal.";
                return RedirectToAction("Index", "Home");
            }

            var licenses = await _doctorPortalService.GetLicensesAsync(userId);

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

            var doctor = await _doctorPortalService.GetDoctorAsync(userId);
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
            if (await _doctorPortalService.HasPendingLicenseAsync(userId))
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

            var license = await _doctorPortalService.SubmitLicenseAsync(userId, licenseNumber, documentUrl, feeAmount);

            return RedirectToAction("PaymentLicense", new { licenseId = license.LicenseId });
        }

        // GET: /DoctorPortal/PaymentLicense/{licenseId}
        [HttpGet]
        public async Task<IActionResult> PaymentLicense(int licenseId)
        {
            var license = await _doctorPortalService.GetLicenseAsync(licenseId);
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
            var license = await _doctorPortalService.GetLicenseAsync(licenseId);
            if (license == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || license.DoctorId != userId)
            {
                return Forbid();
            }

            await _doctorPortalService.PayLicenseAsync(licenseId, userId);

            TempData["SuccessMessage"] = "Verification fee paid successfully! Your licensing documents are now pending administrator approval.";
            return RedirectToAction("License");
        }

        // GET: /DoctorPortal/Availability
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            var slots = await _doctorPortalService.GetAvailabilitySlotsAsync(userId);

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
            if (await _doctorPortalService.HasSlotConflictAsync(userId, startDateTime, endDateTime))
            {
                TempData["ErrorMessage"] = "This time slot conflicts with an existing slot.";
                return RedirectToAction("Availability");
            }

            await _doctorPortalService.AddSlotAsync(userId, startDateTime, endDateTime);

            TempData["SuccessMessage"] = "Availability slot added successfully.";
            return RedirectToAction("Availability");
        }

        // POST: /DoctorPortal/DeleteSlot
        [HttpPost]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            var slot = await _doctorPortalService.GetSlotAsync(slotId);
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

            await _doctorPortalService.DeleteSlotAsync(slotId, userId);

            TempData["SuccessMessage"] = "Slot removed successfully.";
            return RedirectToAction("Availability");
        }

        // GET: /DoctorPortal/Reviews
        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Authentication");

            var doctor = await _doctorPortalService.GetDoctorAsync(userId);
            if (doctor == null) return Forbid();

            var reviews = await _doctorPortalService.GetApprovedReviewsAsync(userId);

            ViewBag.Doctor = doctor;
            return View(reviews);
        }
    }
}
