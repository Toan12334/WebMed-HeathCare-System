using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize] // Require login to request ambulance
    public class EmergencyRequestController : Controller
    {
        private readonly WebMedDbContext _context;

        public EmergencyRequestController(WebMedDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequest(string pickupLocation, string emergencyDetails, decimal? latitude, decimal? longitude)
        {
            if (string.IsNullOrWhiteSpace(pickupLocation))
            {
                ViewBag.Error = "Pickup location is required.";
                return View("Index");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            // Find the patient record for this user
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == userId);
            if (patient == null)
            {
                ViewBag.Error = "You must be registered as a patient to request an ambulance.";
                return View("Index");
            }

            // If coordinates are not provided, fallback to default HCMC coordinates
            decimal patientLat = latitude ?? 10.776m;
            decimal patientLng = longitude ?? 106.700m;

            // Ambulance starts at a nearby hospital (simulated offset of -0.008)
            decimal startLat = patientLat - 0.008m;
            decimal startLng = patientLng - 0.008m;

            string assignedAmbulance = "AMB-1024";

            var request = new AmbulanceRequest
            {
                PatientId = patient.PatientId,
                PickupLocation = pickupLocation,
                Latitude = patientLat,
                Longitude = patientLng,
                Status = "Assigned",
                AssignedAmbulanceVehicle = assignedAmbulance,
                AmbulanceLatitude = startLat,
                AmbulanceLongitude = startLng,
                Eta = "15 mins",
                RequestedAt = DateTime.Now
            };

            _context.AmbulanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Track", new { requestId = request.RequestId });
        }

        [HttpGet]
        public async Task<IActionResult> Track(int requestId)
        {
            var request = await _context.AmbulanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // Simulated GPSService endpoint for tracking
        [HttpGet]
        public async Task<IActionResult> GetTrackingData(int requestId)
        {
            var request = await _context.AmbulanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            // Simulate ambulance moving by slightly changing coordinates if it's assigned
            if (request.Status == "Assigned" || request.Status == "On the way")
            {
                // Calculate route progression from starting hospital to patient location (cycle every 3 minutes)
                double elapsedSeconds = (DateTime.Now - request.RequestedAt).TotalSeconds;
                double steps = (elapsedSeconds % 180) / 180.0; // 0.0 to 1.0

                decimal destLat = request.Latitude ?? 10.776m;
                decimal destLng = request.Longitude ?? 106.700m;

                decimal startLat = destLat - 0.008m;
                decimal startLng = destLng - 0.008m;

                request.AmbulanceLatitude = startLat + (destLat - startLat) * (decimal)steps;
                request.AmbulanceLongitude = startLng + (destLng - startLng) * (decimal)steps;
                
                // Calculate ETA progressively based on elapsed time since request was made
                int remainingMinutes = 15 - (int)(elapsedSeconds / 60);
                if (remainingMinutes < 1) remainingMinutes = 1; // Floor to 1 minute
                request.Eta = $"{remainingMinutes} mins";

                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                latitude = request.AmbulanceLatitude,
                longitude = request.AmbulanceLongitude,
                status = request.Status,
                eta = request.Eta,
                vehicle = request.AssignedAmbulanceVehicle
            });
        }
    }
}
