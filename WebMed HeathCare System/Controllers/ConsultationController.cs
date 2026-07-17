using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class ConsultationController : Controller
    {
        private readonly WebMedDbContext _context;

        public ConsultationController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /Consultation/Session/{appointmentId}
        [HttpGet]
        public async Task<IActionResult> Session(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.PatientNavigation)
                .Include(a => a.Doctor).ThenInclude(d => d.DoctorNavigation)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

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

            var medicines = await _context.Medicines
                .Where(m => m.IsActive && m.Name.Contains(keyword))
                .Select(m => new { m.MedicineId, m.Name, m.Price })
                .Take(5)
                .ToListAsync();

            return Json(medicines);
        }

        // POST: /Consultation/SubmitConsultation
        [HttpPost]
        public async Task<IActionResult> SubmitConsultation(int appointmentId, string diagnosis, string treatmentPlan, string prescriptionJson)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || appointment.DoctorId != userId)
            {
                return Forbid();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Update Appointment Status
                    appointment.Status = "Completed";

                    // 2. Create Consultation Outcome
                    var consultation = new Consultation
                    {
                        AppointmentId = appointmentId,
                        Diagnosis = diagnosis ?? "No diagnosis entered.",
                        TreatmentPlan = treatmentPlan,
                        ConductedAt = DateTime.Now
                    };
                    _context.Consultations.Add(consultation);
                    await _context.SaveChangesAsync(); // Generates ConsultationId

                    // 3. Create Prescription if medicines are selected
                    if (!string.IsNullOrWhiteSpace(prescriptionJson))
                    {
                        var items = JsonSerializer.Deserialize<List<PrescriptionInputItem>>(prescriptionJson);
                        if (items != null && items.Any())
                        {
                            var prescription = new Prescription
                            {
                                ConsultationId = consultation.ConsultationId,
                                DoctorId = appointment.DoctorId,
                                PatientId = appointment.PatientId,
                                IssuedDate = DateTime.Now,
                                Notes = "Consultation Prescription"
                            };

                            _context.Prescriptions.Add(prescription);
                            await _context.SaveChangesAsync(); // Generates PrescriptionId

                            foreach (var item in items)
                            {
                                var presItem = new PrescriptionItem
                                {
                                    PrescriptionId = prescription.PrescriptionId,
                                    MedicineId = item.MedicineId,
                                    Dosage = item.Dosage ?? "As directed",
                                    Frequency = item.Frequency ?? "Once daily",
                                    DurationDays = item.DurationDays > 0 ? item.DurationDays : 7
                                };
                                _context.PrescriptionItems.Add(presItem);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Consultation completed successfully.";
                    return RedirectToAction("Receipt", new { appointmentId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Failed to submit consultation details. Please try again.";
                    return RedirectToAction("Session", new { appointmentId });
                }
            }
        }

        // GET: /Consultation/Receipt/{appointmentId}
        [HttpGet]
        public async Task<IActionResult> Receipt(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.PatientNavigation)
                .Include(a => a.Doctor).ThenInclude(d => d.DoctorNavigation)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null) return NotFound();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

            if (consultation == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(p => p.ConsultationId == consultation.ConsultationId);

            ViewBag.Consultation = consultation;
            ViewBag.Prescription = prescription;

            return View(appointment);
        }
    }

    public class PrescriptionInputItem
    {
        public int MedicineId { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public int DurationDays { get; set; }
    }
}
