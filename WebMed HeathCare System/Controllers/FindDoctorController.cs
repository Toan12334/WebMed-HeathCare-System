using Microsoft.AspNetCore.Mvc;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    public class FindDoctorController : Controller
    {
        private readonly IFindDoctorService _findDoctorService;

        public FindDoctorController(IFindDoctorService findDoctorService)
        {
            _findDoctorService = findDoctorService;
        }

        // GET: /FindDoctor
        // GET: /FindDoctor
        public async Task<IActionResult> Index(string specialty, string location, string rank, string position, string searchTerm)
        {
            var doctors = await _findDoctorService.SearchDoctorsAsync(specialty, location, rank, position, searchTerm);

            // Populate filters
            ViewBag.Specialties = await _findDoctorService.GetSpecialtiesAsync();
            ViewBag.Locations = await _findDoctorService.GetLocationsAsync();

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
            var doctor = await _findDoctorService.GetDoctorDetailsAsync(id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Get consultation fee from DoctorLicenses, default to 150000 VND if none
            ViewBag.Fee = await _findDoctorService.GetConsultationFeeAsync(id);

            return View(doctor);
        }
    }
}
