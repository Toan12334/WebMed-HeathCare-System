using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IAppointmentService
    {
        Task<bool> IsUnverifiedDoctorAsync(int userId);
        Task<List<Appointment>> GetAppointmentsForUserAsync(int userId, string? role);
        Task<Patient?> GetPatientAsync(int userId);
        Task<AvailabilitySlot?> GetAvailableSlotAsync(int slotId);
        Task<bool> BookAppointmentAsync(int patientId, AvailabilitySlot slot);
    }
}
