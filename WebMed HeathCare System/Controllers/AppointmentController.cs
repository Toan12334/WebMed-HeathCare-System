using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly WebMedDbContext _context;

        public AppointmentController(WebMedDbContext context)
        {
            _context = context;
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

            List<Appointment> appointments;

            if (role == "Doctor")
            {
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.PatientNavigation)
                    .Include(a => a.Slot)
                    .Where(a => a.DoctorId == userId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();
            }
            else
            {
                appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .ThenInclude(d => d.DoctorNavigation)
                    .Include(a => a.Slot)
                    .Where(a => a.PatientId == userId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();
            }

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

            var patient = await _context.Patients.FindAsync(userId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Only patients can book appointments.";
                return RedirectToAction("Index", "FindDoctor");
            }

            var slot = await _context.AvailabilitySlots.FindAsync(slotId);
            if (slot == null || slot.IsBooked || !slot.IsActive)
            {
                TempData["ErrorMessage"] = "This time slot is no longer available.";
                return RedirectToAction("Index", "FindDoctor");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Reserve the slot
                    slot.IsBooked = true;

                    var appointment = new Appointment
                    {
                        PatientId = userId,
                        DoctorId = slot.DoctorId,
                        SlotId = slotId,
                        AppointmentDateTime = slot.StartDateTime,
                        Status = "Scheduled",
                        ConsultationType = "Online",
                        CreatedAt = DateTime.Now
                    };

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Appointment booked successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while booking. Please try again.";
                    return RedirectToAction("Details", "FindDoctor", new { id = slot.DoctorId });
                }
            }
        }
    }
}
