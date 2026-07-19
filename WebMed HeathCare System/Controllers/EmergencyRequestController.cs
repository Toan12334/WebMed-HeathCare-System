using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    public class EmergencyRequestController : Controller
    {
        private readonly IEmergencyRequestService _emergencyRequestService;

        public EmergencyRequestController(IEmergencyRequestService emergencyRequestService)
        {
            _emergencyRequestService = emergencyRequestService;
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
