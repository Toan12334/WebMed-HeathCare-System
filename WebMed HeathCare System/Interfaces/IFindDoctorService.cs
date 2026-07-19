using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IFindDoctorService
    {
        Task<List<Doctor>> SearchDoctorsAsync(string? specialty, string? location, string? rank, string? position, string? searchTerm);
        Task<List<string>> GetSpecialtiesAsync();
        Task<List<string>> GetLocationsAsync();
        Task<Doctor?> GetDoctorDetailsAsync(int id);
        Task<decimal> GetConsultationFeeAsync(int doctorId);
    }
}
