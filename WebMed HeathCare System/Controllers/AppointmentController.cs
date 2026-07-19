using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // GET: /Appointment
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role == "Doctor" && await _appointmentService.IsUnverifiedDoctorAsync(userId))
            {
                TempData["ErrorMessage"] = "You must submit and get your professional license approved by the administrator before accessing other features.";
                return RedirectToAction("License", "DoctorPortal");
            }

            var appointments = await _appointmentService.GetAppointmentsForUserAsync(userId, role);
            return View(appointments);
        }

        // POST: /Appointment/Book
        [HttpPost]
        public async Task<IActionResult> Book(int slotId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var patient = await _appointmentService.GetPatientAsync(userId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Only patients can book appointments.";
                return RedirectToAction("Index", "FindDoctor");
            }

            var slot = await _appointmentService.GetAvailableSlotAsync(slotId);
            if (slot == null)
            {
                TempData["ErrorMessage"] = "This time slot is no longer available.";
                return RedirectToAction("Index", "FindDoctor");
            }

            var success = await _appointmentService.BookAppointmentAsync(userId, slot);
            if (success)
            {
                TempData["SuccessMessage"] = "Appointment booked successfully!";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "An error occurred while booking. Please try again.";
            return RedirectToAction("Details", "FindDoctor", new { id = slot.DoctorId });
        }
    }
}
