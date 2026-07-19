using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class FindDoctorService : IFindDoctorService
    {
        private readonly WebMedDbContext _context;

        public FindDoctorService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<Doctor>> SearchDoctorsAsync(string? specialty, string? location, string? rank, string? position, string? searchTerm)
        {
            var query = _context.Doctors
                .Include(d => d.DoctorNavigation)
                .Where(d => d.IsActive && d.IsVerified);

            if (!string.IsNullOrEmpty(specialty))
            {
                query = query.Where(d => d.Specialty == specialty);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(d => d.Location!.Contains(location));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.DoctorNavigation.FullName.Contains(searchTerm) || d.Bio!.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(rank))
            {
                query = query.Where(d => d.DoctorNavigation.FullName.Contains(rank));
            }

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(d => d.Bio!.Contains(position) || d.DoctorNavigation.FullName.Contains(position));
            }

            return await query.ToListAsync();
        }

        public async Task<List<string>> GetSpecialtiesAsync()
        {
            return await _context.Doctors
                .Where(d => d.IsActive && d.IsVerified)
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<string>> GetLocationsAsync()
        {
            return await _context.Doctors
                .Where(d => d.IsActive && d.IsVerified && d.Location != null)
                .Select(d => d.Location!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Doctor?> GetDoctorDetailsAsync(int id)
        {
            return await _context.Doctors
                .Include(d => d.DoctorNavigation)
                .Include(d => d.AvailabilitySlots.Where(s => s.IsActive && !s.IsBooked && s.StartDateTime > DateTime.Now))
                .FirstOrDefaultAsync(d => d.DoctorId == id && d.IsActive && d.IsVerified);
        }

        public async Task<decimal> GetConsultationFeeAsync(int doctorId)
        {
            var license = await _context.DoctorLicenses
                .FirstOrDefaultAsync(l => l.DoctorId == doctorId && l.VerificationStatus == "Approved");

            return license?.FeeAmount ?? 150000m;
        }
    }
}
