using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly WebMedDbContext _context;

        public AppointmentService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsUnverifiedDoctorAsync(int userId)
        {
            var doctor = await _context.Doctors.FindAsync(userId);
            return doctor != null && !doctor.IsVerified;
        }

        public async Task<List<Appointment>> GetAppointmentsForUserAsync(int userId, string? role)
        {
            if (role == "Doctor")
            {
                return await _context.Appointments
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.PatientNavigation)
                    .Include(a => a.Slot)
                    .Where(a => a.DoctorId == userId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();
            }

            return await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.DoctorNavigation)
                .Include(a => a.Slot)
                .Where(a => a.PatientId == userId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        public async Task<Patient?> GetPatientAsync(int userId)
        {
            return await _context.Patients.FindAsync(userId);
        }

        public async Task<AvailabilitySlot?> GetAvailableSlotAsync(int slotId)
        {
            var slot = await _context.AvailabilitySlots.FindAsync(slotId);
            if (slot == null || slot.IsBooked || !slot.IsActive)
            {
                return null;
            }

            return slot;
        }

        public async Task<bool> BookAppointmentAsync(int patientId, AvailabilitySlot slot)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                slot.IsBooked = true;

                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = slot.DoctorId,
                    SlotId = slot.SlotId,
                    AppointmentDateTime = slot.StartDateTime,
                    Status = "Scheduled",
                    ConsultationType = "Online",
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
