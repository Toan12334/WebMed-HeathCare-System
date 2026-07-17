using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class FindDoctorController : Controller
    {
        private readonly WebMedDbContext _context;

        public FindDoctorController(WebMedDbContext context)
        {
            _context = context;
        }

        // GET: /FindDoctor
        public async Task<IActionResult> Index(string specialty, string location)
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

            var doctors = await query.ToListAsync();

            // Populate filters
            ViewBag.Specialties = await _context.Doctors
                .Where(d => d.IsActive && d.IsVerified)
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();

            ViewBag.Locations = await _context.Doctors
                .Where(d => d.IsActive && d.IsVerified && d.Location != null)
                .Select(d => d.Location)
                .Distinct()
                .ToListAsync();

            ViewBag.SelectedSpecialty = specialty;
            ViewBag.SelectedLocation = location;

            return View(doctors);
        }

        // GET: /FindDoctor/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.DoctorNavigation)
                .Include(d => d.AvailabilitySlots.Where(s => s.IsActive && !s.IsBooked && s.StartDateTime > DateTime.Now))
                .FirstOrDefaultAsync(d => d.DoctorId == id && d.IsActive);

            if (doctor == null)
            {
                return NotFound();
            }

            // Get consultation fee from DoctorLicenses, default to 150000 VND if none
            var license = await _context.DoctorLicenses
                .FirstOrDefaultAsync(l => l.DoctorId == id && l.VerificationStatus == "Approved");
            ViewBag.Fee = license?.FeeAmount ?? 150000m;

            return View(doctor);
        }
    }
}
