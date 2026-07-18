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
        // GET: /FindDoctor
        public async Task<IActionResult> Index(string specialty, string location, string rank, string position, string searchTerm)
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

            ViewBag.Ranks = new List<string> { "Prof.", "Assoc. Prof.", "PhD", "MD" };
            ViewBag.Positions = new List<string> { "director", "expert", "specialist", "advisor" };

            ViewBag.SelectedSpecialty = specialty;
            ViewBag.SelectedLocation = location;
            ViewBag.SelectedRank = rank;
            ViewBag.SelectedPosition = position;
            ViewBag.SearchTerm = searchTerm;

            return View(doctors);
        }

        // GET: /FindDoctor/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.DoctorNavigation)
                .Include(d => d.AvailabilitySlots.Where(s => s.IsActive && !s.IsBooked && s.StartDateTime > DateTime.Now))
                .FirstOrDefaultAsync(d => d.DoctorId == id && d.IsActive && d.IsVerified);

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
