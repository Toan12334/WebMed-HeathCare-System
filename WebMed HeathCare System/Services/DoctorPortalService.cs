using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class DoctorPortalService : IDoctorPortalService
    {
        private readonly WebMedDbContext _context;

        public DoctorPortalService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsUnverifiedDoctorAsync(int userId)
        {
            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor == null) return true;

            // Doctor is verified if the flag is true OR they have at least one approved license
            if (doctor.IsVerified) return false;

            var hasApprovedLicense = await _context.DoctorLicenses.AnyAsync(l => l.DoctorId == userId && l.VerificationStatus == "Approved");
            return !hasApprovedLicense;
        }

        public async Task<Doctor?> GetDoctorAsync(int userId)
        {
            return await _context.Doctors.FindAsync(userId);
        }

        public async Task<List<DoctorLicense>> GetLicensesAsync(int doctorId)
        {
            return await _context.DoctorLicenses
                .Where(l => l.DoctorId == doctorId)
                .OrderByDescending(l => l.SubmittedAt)
                .ToListAsync();
        }

        public async Task<bool> HasPendingLicenseAsync(int doctorId)
        {
            return await _context.DoctorLicenses.AnyAsync(l => l.DoctorId == doctorId &&
                (l.VerificationStatus == "Pending" || l.PaymentStatus == "Pending"));
        }

        public async Task<DoctorLicense> SubmitLicenseAsync(int doctorId, string licenseNumber, string documentUrl, decimal feeAmount)
        {
            var license = new DoctorLicense
            {
                DoctorId = doctorId,
                LicenseNumber = licenseNumber,
                DocumentUrl = documentUrl,
                FeeAmount = feeAmount > 0 ? feeAmount : 150000m,
                PaymentStatus = "Pending",
                VerificationStatus = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.DoctorLicenses.Add(license);
            await _context.SaveChangesAsync();

            return license;
        }

        public async Task<DoctorLicense?> GetLicenseAsync(int licenseId)
        {
            return await _context.DoctorLicenses.FindAsync(licenseId);
        }

        public async Task<bool> PayLicenseAsync(int licenseId, int doctorId)
        {
            var license = await _context.DoctorLicenses.FindAsync(licenseId);
            if (license == null || license.DoctorId != doctorId)
            {
                return false;
            }

            license.PaymentStatus = "Paid";
            license.VerificationStatus = "Pending";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AvailabilitySlot>> GetAvailabilitySlotsAsync(int doctorId)
        {
            return await _context.AvailabilitySlots
                .Where(s => s.DoctorId == doctorId && s.IsActive)
                .OrderBy(s => s.StartDateTime)
                .ToListAsync();
        }

        public async Task<bool> HasSlotConflictAsync(int doctorId, DateTime startDateTime, DateTime endDateTime)
        {
            return await _context.AvailabilitySlots
                .AnyAsync(s => s.DoctorId == doctorId && s.IsActive &&
                               ((startDateTime >= s.StartDateTime && startDateTime < s.EndDateTime) ||
                                (endDateTime > s.StartDateTime && endDateTime <= s.EndDateTime) ||
                                (startDateTime <= s.StartDateTime && endDateTime >= s.EndDateTime)));
        }

        public async Task AddSlotAsync(int doctorId, DateTime startDateTime, DateTime endDateTime)
        {
            var slot = new AvailabilitySlot
            {
                DoctorId = doctorId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                IsBooked = false,
                IsActive = true
            };

            _context.AvailabilitySlots.Add(slot);
            await _context.SaveChangesAsync();
        }

        public async Task<AvailabilitySlot?> GetSlotAsync(int slotId)
        {
            return await _context.AvailabilitySlots.FindAsync(slotId);
        }

        public async Task<bool> DeleteSlotAsync(int slotId, int doctorId)
        {
            var slot = await _context.AvailabilitySlots.FindAsync(slotId);
            if (slot == null || slot.DoctorId != doctorId || slot.IsBooked)
            {
                return false;
            }

            slot.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DoctorReview>> GetApprovedReviewsAsync(int doctorId)
        {
            return await _context.DoctorReviews
                .Include(r => r.Patient)
                .ThenInclude(p => p.PatientNavigation)
                .Where(r => r.DoctorId == doctorId && r.ModerationStatus == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
