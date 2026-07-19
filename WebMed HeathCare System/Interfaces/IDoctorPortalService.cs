using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IDoctorPortalService
    {
        Task<bool> IsUnverifiedDoctorAsync(int userId);
        Task<Doctor?> GetDoctorAsync(int userId);
        Task<List<DoctorLicense>> GetLicensesAsync(int doctorId);
        Task<bool> HasPendingLicenseAsync(int doctorId);
        Task<DoctorLicense> SubmitLicenseAsync(int doctorId, string licenseNumber, string documentUrl, decimal feeAmount);
        Task<DoctorLicense?> GetLicenseAsync(int licenseId);
        Task<bool> PayLicenseAsync(int licenseId, int doctorId);
        Task<List<AvailabilitySlot>> GetAvailabilitySlotsAsync(int doctorId);
        Task<bool> HasSlotConflictAsync(int doctorId, DateTime startDateTime, DateTime endDateTime);
        Task AddSlotAsync(int doctorId, DateTime startDateTime, DateTime endDateTime);
        Task<AvailabilitySlot?> GetSlotAsync(int slotId);
        Task<bool> DeleteSlotAsync(int slotId, int doctorId);
        Task<List<DoctorReview>> GetApprovedReviewsAsync(int doctorId);
    }
}
