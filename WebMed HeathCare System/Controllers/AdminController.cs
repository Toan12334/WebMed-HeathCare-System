using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> Users(string searchEmail)
        {
            ViewBag.SearchEmail = searchEmail;
            ViewBag.Roles = await _adminService.GetRolesAsync();
            return View(await _adminService.GetUsersAsync(searchEmail));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _adminService.GetUserDetailsAsync(id);
            if (user == null) return NotFound();

            return Json(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string username, string password, string fullName, string email, string phoneNumber, int roleId, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            var result = await _adminService.CreateUserAsync(username, password, fullName, email, phoneNumber, roleId, specialty, location, bio, gender, address, bloodType, dob);
            SetTempMessage(result.Success, result.Message);
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(int userId, string fullName, string email, string phoneNumber, int roleId, string password, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            var result = await _adminService.EditUserAsync(userId, fullName, email, phoneNumber, roleId, password, specialty, location, bio, gender, address, bloodType, dob);
            SetTempMessage(result.Success, result.Message);
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var result = await _adminService.ToggleUserActiveAsync(id);
            if (!result.Success) return NotFound();

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _adminService.SoftDeleteUserAsync(id);
            if (!result.Success) return NotFound();

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> Doctors()
        {
            return View(await _adminService.GetPendingDoctorLicensesAsync());
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDoctor(int licenseId, string status)
        {
            int? adminId = null;
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(adminIdString, out int parsedAdminId))
            {
                adminId = parsedAdminId;
            }

            if (!await _adminService.VerifyDoctorAsync(licenseId, status, adminId)) return NotFound();

            TempData["SuccessMessage"] = $"Doctor license verification request has been {status}.";
            return RedirectToAction("Doctors");
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            return View(await _adminService.GetReviewsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ModerateReview(int id, string status)
        {
            if (!await _adminService.ModerateReviewAsync(id, status)) return NotFound();

            TempData["SuccessMessage"] = $"Review moderation status updated to {status}.";
            return RedirectToAction("Reviews");
        }

        [HttpGet]
        public async Task<IActionResult> News()
        {
            return View(await _adminService.GetNewsAsync());
        }

        [HttpGet]
        public IActionResult CreateNews()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateNews(string title, string category, string content, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Title and Content are required.";
                return View();
            }

            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authorIdString, out int authorId)) return Forbid();

            await _adminService.CreateNewsAsync(title, category, content, imageUrl, authorId);

            TempData["SuccessMessage"] = "Health article published successfully!";
            return RedirectToAction("News");
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            return View(await _adminService.GetRolesAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName, string description)
        {
            var result = await _adminService.CreateRoleAsync(roleName, description);
            SetTempMessage(result.Success, result.Message);
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(int roleId, string roleName, string description)
        {
            var result = await _adminService.EditRoleAsync(roleId, roleName, description);
            SetTempMessage(result.Success, result.Message);
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var result = await _adminService.DeleteRoleAsync(id);
            SetTempMessage(result.Success, result.Message);
            return RedirectToAction("Roles");
        }

        [HttpGet]
        public async Task<IActionResult> Emergencies()
        {
            return View(await _adminService.GetEmergencyRequestsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> DispatchAmbulance(int requestId, string vehicleNumber)
        {
            if (!await _adminService.DispatchAmbulanceAsync(requestId, vehicleNumber)) return NotFound();

            TempData["SuccessMessage"] = $"Ambulance vehicle {vehicleNumber} dispatched successfully.";
            return RedirectToAction("Emergencies");
        }

        private void SetTempMessage(bool success, string message)
        {
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        }
    }
}
