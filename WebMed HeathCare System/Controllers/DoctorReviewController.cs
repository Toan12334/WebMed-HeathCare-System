using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class DoctorReviewController : Controller
    {
        private readonly WebMedDbContext _context;

        public DoctorReviewController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /DoctorReview/Create?appointmentId=5
        [HttpGet]
        public async Task<IActionResult> Create(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.DoctorNavigation)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

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
            var consultation = await _context.Consultations.FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Consultation record not found.";
                return RedirectToAction("Index", "Appointment");
            }

            var alreadyReviewed = await _context.DoctorReviews.AnyAsync(r => r.ConsultationId == consultation.ConsultationId);
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
            var appointment = await _context.Appointments.FindAsync(appointmentId);
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

            var review = new DoctorReview
            {
                PatientId = userId,
                DoctorId = appointment.DoctorId,
                ConsultationId = consultationId,
                Rating = rating,
                Comment = comment,
                ModerationStatus = "Approved",
                CreatedAt = DateTime.Now
            };

            _context.DoctorReviews.Add(review);
            await _context.SaveChangesAsync();

            // Recalculate average rating for the doctor
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);
            if (doctor != null)
            {
                var ratings = await _context.DoctorReviews
                    .Where(r => r.DoctorId == doctor.DoctorId && r.ModerationStatus == "Approved")
                    .Select(r => r.Rating)
                    .ToListAsync();

                if (ratings.Any())
                {
                    doctor.AverageRating = (decimal)ratings.Average();
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Thank you! Your feedback has been submitted successfully.";
            return RedirectToAction("Index", "Appointment");
        }
    }
}
