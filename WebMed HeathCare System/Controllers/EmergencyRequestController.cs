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
        public async Task<IActionResult> SubmitRequest(string pickupLocation, string emergencyDetails)
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

            // Simulate Ambulance Dispatcher finding an ambulance
            string assignedAmbulance = "AMB-1024";
            decimal startLat = 10.762622m; // Example starting point (HCMC)
            decimal startLng = 106.660172m;

            var request = new AmbulanceRequest
            {
                PatientId = patient.PatientId,
                PickupLocation = pickupLocation,
                Status = "Assigned",
                AssignedAmbulanceVehicle = assignedAmbulance,
                AmbulanceLatitude = startLat,
                AmbulanceLongitude = startLng,
                Eta = "15 mins",
                RequestedAt = DateTime.Now
                // Note: We don't have an emergencyDetails column in AmbulanceRequest, but in a real app we'd save it.
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
                // In a real app, GPSService would update the DB or send live events.
                // Here we just slightly adjust the mock coordinates to simulate movement.
                request.AmbulanceLatitude += 0.0001m;
                request.AmbulanceLongitude += 0.0001m;
                
                // Randomize ETA for simulation
                int mins = new Random().Next(2, 14);
                request.Eta = $"{mins} mins";

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
