using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class EmergencyRequestController : Controller
    {
        private readonly IEmergencyRequestService _emergencyRequestService;
        private readonly WebMedDbContext _context;

        public EmergencyRequestController(IEmergencyRequestService emergencyRequestService, WebMedDbContext context)
        {
            _emergencyRequestService = emergencyRequestService;
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
            int? patientId = null;
            if (int.TryParse(userIdString, out int userId))
            {
                patientId = userId;
            }

            var request = await _emergencyRequestService.CreateRequestAsync(patientId, pickupLocation, emergencyDetails, latitude, longitude);

            return RedirectToAction("Track", new { requestId = request.RequestId });
        }

        [HttpGet]
        public async Task<IActionResult> Track(int requestId)
        {
            var request = await _emergencyRequestService.GetRequestByIdAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> GetTrackingData(int requestId)
        {
            var trackingData = await _emergencyRequestService.GetTrackingDataAsync(requestId);
            if (trackingData == null)
            {
                return NotFound();
            }

            return Json(trackingData);
        }
    }
}
