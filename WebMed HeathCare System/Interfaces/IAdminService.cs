using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IAdminService
    {
        Task<List<User>> GetUsersAsync(string? searchEmail);
        Task<List<Role>> GetRolesAsync();
        Task<object?> GetUserDetailsAsync(int id);
        Task<(bool Success, string Message)> CreateUserAsync(string username, string password, string fullName, string email, string phoneNumber, int roleId, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob);
        Task<(bool Success, string Message)> EditUserAsync(int userId, string fullName, string email, string phoneNumber, int roleId, string password, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob);
        Task<(bool Success, string Message)> ToggleUserActiveAsync(int id);
        Task<(bool Success, string Message)> SoftDeleteUserAsync(int id);
        Task<List<DoctorLicense>> GetPendingDoctorLicensesAsync();
        Task<bool> VerifyDoctorAsync(int licenseId, string status, int? adminId);
        Task<List<DoctorReview>> GetReviewsAsync();
        Task<bool> ModerateReviewAsync(int id, string status);
        Task<List<Article>> GetNewsAsync();
        Task<bool> CreateNewsAsync(string title, string? category, string content, string? imageUrl, int authorId);
        Task<(bool Success, string Message)> CreateRoleAsync(string roleName, string description);
        Task<(bool Success, string Message)> EditRoleAsync(int roleId, string roleName, string description);
        Task<(bool Success, string Message)> DeleteRoleAsync(int id);
        Task<List<AmbulanceRequest>> GetEmergencyRequestsAsync();
        Task<bool> DispatchAmbulanceAsync(int requestId, string? vehicleNumber);
    }
}
