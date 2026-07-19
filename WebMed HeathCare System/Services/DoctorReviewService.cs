using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class DoctorReviewService : IDoctorReviewService
    {
        private readonly WebMedDbContext _context;

        public DoctorReviewService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetAppointmentWithDoctorAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.DoctorNavigation)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }

        public async Task<Appointment?> GetAppointmentAsync(int appointmentId)
        {
            return await _context.Appointments.FindAsync(appointmentId);
        }

        public async Task<Consultation?> GetConsultationByAppointmentAsync(int appointmentId)
        {
            return await _context.Consultations.FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
        }

        public async Task<bool> HasReviewForConsultationAsync(int consultationId)
        {
            return await _context.DoctorReviews.AnyAsync(r => r.ConsultationId == consultationId);
        }

        public async Task CreateReviewAsync(int patientId, int doctorId, int consultationId, int rating, string comment)
        {
            var review = new DoctorReview
            {
                PatientId = patientId,
                DoctorId = doctorId,
                ConsultationId = consultationId,
                Rating = rating,
                Comment = comment,
                ModerationStatus = "Approved",
                CreatedAt = DateTime.Now
            };

            _context.DoctorReviews.Add(review);
            await _context.SaveChangesAsync();

            var doctor = await _context.Doctors.FindAsync(doctorId);
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
        }
    }
}
