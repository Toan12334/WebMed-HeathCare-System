using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IDoctorReviewService
    {
        Task<Appointment?> GetAppointmentWithDoctorAsync(int appointmentId);
        Task<Appointment?> GetAppointmentAsync(int appointmentId);
        Task<Consultation?> GetConsultationByAppointmentAsync(int appointmentId);
        Task<bool> HasReviewForConsultationAsync(int consultationId);
        Task CreateReviewAsync(int patientId, int doctorId, int consultationId, int rating, string comment);
    }
}
