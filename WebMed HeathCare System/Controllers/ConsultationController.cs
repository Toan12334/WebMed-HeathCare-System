using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class ConsultationController : Controller
    {
        private readonly IConsultationService _consultationService;

        public ConsultationController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }

        // GET: /Consultation/Session/{appointmentId}
        [HttpGet]
        public async Task<IActionResult> Session(int appointmentId)
        {
            var appointment = await _consultationService.GetAppointmentForSessionAsync(appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || (appointment.PatientId != userId && appointment.DoctorId != userId))
            {
                return Forbid();
            }

            ViewBag.Role = User.FindFirstValue(ClaimTypes.Role);
            return View(appointment);
        }

        // GET: /Consultation/SearchMedicine
        [HttpGet]
        public async Task<IActionResult> SearchMedicine(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new List<object>());
            }

            var medicines = await _consultationService.SearchMedicinesAsync(keyword);

            return Json(medicines);
        }

        // POST: /Consultation/SubmitConsultation
        [HttpPost]
        public async Task<IActionResult> SubmitConsultation(int appointmentId, string diagnosis, string treatmentPlan, string prescriptionJson)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Forbid();
            }

            var success = await _consultationService.SubmitConsultationAsync(appointmentId, userId, diagnosis, treatmentPlan, prescriptionJson);
            if (success)
            {
                TempData["SuccessMessage"] = "Consultation completed successfully.";
                return RedirectToAction("Receipt", new { appointmentId });
            }

            TempData["ErrorMessage"] = "Failed to submit consultation details. Please try again.";
            return RedirectToAction("Session", new { appointmentId });
        }

        // GET: /Consultation/Receipt/{appointmentId}
        [HttpGet]
        public async Task<IActionResult> Receipt(int appointmentId)
        {
            var appointment = await _consultationService.GetAppointmentForSessionAsync(appointmentId);

            if (appointment == null) return NotFound();

            var consultation = await _consultationService.GetConsultationByAppointmentAsync(appointmentId);

            if (consultation == null) return NotFound();

            var prescription = await _consultationService.GetPrescriptionByConsultationAsync(consultation.ConsultationId);

            ViewBag.Consultation = consultation;
            ViewBag.Prescription = prescription;

            return View(appointment);
        }
    }

}
