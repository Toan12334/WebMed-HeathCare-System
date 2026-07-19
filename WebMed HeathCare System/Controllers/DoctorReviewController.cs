using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class DoctorReviewController : Controller
    {
        private readonly IDoctorReviewService _doctorReviewService;

        public DoctorReviewController(IDoctorReviewService doctorReviewService)
        {
            _doctorReviewService = doctorReviewService;
        }

        // GET: /DoctorReview/Create?appointmentId=5
        [HttpGet]
        public async Task<IActionResult> Create(int appointmentId)
        {
            var appointment = await _doctorReviewService.GetAppointmentWithDoctorAsync(appointmentId);

            if (appointment == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || appointment.PatientId != userId)
            {
                return Forbid();
            }

            if (appointment.Status != "Completed")
            {
                TempData["ErrorMessage"] = "You can only review completed appointments.";
                return RedirectToAction("Index", "Appointment");
            }

            // Check if already reviewed
            var consultation = await _doctorReviewService.GetConsultationByAppointmentAsync(appointmentId);
            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Consultation record not found.";
                return RedirectToAction("Index", "Appointment");
            }

            var alreadyReviewed = await _doctorReviewService.HasReviewForConsultationAsync(consultation.ConsultationId);
            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "You have already submitted a review for this consultation.";
                return RedirectToAction("Index", "Appointment");
            }

            ViewBag.Appointment = appointment;
            ViewBag.ConsultationId = consultation.ConsultationId;

            return View();
        }

        // POST: /DoctorReview/Create
        [HttpPost]
        public async Task<IActionResult> Create(int appointmentId, int consultationId, int rating, string comment)
        {
            var appointment = await _doctorReviewService.GetAppointmentAsync(appointmentId);
            if (appointment == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || appointment.PatientId != userId)
            {
                return Forbid();
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction("Create", new { appointmentId });
            }

            await _doctorReviewService.CreateReviewAsync(userId, appointment.DoctorId, consultationId, rating, comment);

            TempData["SuccessMessage"] = "Thank you! Your feedback has been submitted successfully.";
            return RedirectToAction("Index", "Appointment");
        }
    }
}
