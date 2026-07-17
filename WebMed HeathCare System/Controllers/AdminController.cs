using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly WebMedDbContext _context;

        public AdminController(WebMedDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // UC 27: USER MANAGEMENT
        // ==========================================

        // GET: /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users(string searchEmail)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail) || u.FullName.Contains(searchEmail));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.SearchEmail = searchEmail;

            return View(users);
        }

        // POST: /Admin/SuspendUser
        [HttpPost]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Toggle IsActive status
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.FullName}' account active status changed to {user.IsActive}.";
            return RedirectToAction("Users");
        }


        // ==========================================
        // UC 30: DOCTOR VERIFICATION
        // ==========================================

        // GET: /Admin/Doctors
        [HttpGet]
        public async Task<IActionResult> Doctors()
        {
            // Get pending licenses
            var pendingLicenses = await _context.DoctorLicenses
                .Include(l => l.Doctor)
                .ThenInclude(d => d.DoctorNavigation)
                .Where(l => l.VerificationStatus == "Pending")
                .ToListAsync();

            return View(pendingLicenses);
        }

        // POST: /Admin/VerifyDoctor
        [HttpPost]
        public async Task<IActionResult> VerifyDoctor(int licenseId, string status)
        {
            var license = await _context.DoctorLicenses.FindAsync(licenseId);
            if (license == null) return NotFound();

            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(adminIdString, out int adminId))
            {
                license.ReviewedBy = adminId;
            }

            license.VerificationStatus = status; // "Approved" or "Rejected"
            license.ReviewedAt = DateTime.Now;

            // If approved, verify the doctor
            var doctor = await _context.Doctors.FindAsync(license.DoctorId);
            if (doctor != null)
            {
                doctor.IsVerified = status == "Approved";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Doctor license verification request has been {status}.";
            return RedirectToAction("Doctors");
        }


        // ==========================================
        // UC 31: REVIEW MODERATION
        // ==========================================

        // GET: /Admin/Reviews
        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.DoctorReviews
                .Include(r => r.Patient).ThenInclude(p => p.PatientNavigation)
                .Include(r => r.Doctor).ThenInclude(d => d.DoctorNavigation)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        // POST: /Admin/ModerateReview
        [HttpPost]
        public async Task<IActionResult> ModerateReview(int id, string status)
        {
            var review = await _context.DoctorReviews.FindAsync(id);
            if (review == null) return NotFound();

            review.ModerationStatus = status; // "Approved" or "Rejected" / "Hidden"
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Review moderation status updated to {status}.";
            return RedirectToAction("Reviews");
        }


        // ==========================================
        // UC 28: HEALTH NEWS PUBLISHING
        // ==========================================

        // GET: /Admin/News
        [HttpGet]
        public async Task<IActionResult> News()
        {
            var news = await _context.Articles
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return View(news);
        }

        // GET: /Admin/CreateNews
        [HttpGet]
        public IActionResult CreateNews()
        {
            return View();
        }

        // POST: /Admin/CreateNews
        [HttpPost]
        public async Task<IActionResult> CreateNews(string title, string category, string content)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Title and Content are required.";
                return View();
            }

            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authorIdString, out int authorId)) return Forbid();

            var article = new Article
            {
                Title = title,
                Category = category ?? "General",
                Content = content,
                AuthorId = authorId,
                IsPublished = true, // Directly publish for ease of testing
                IsActive = true,
                PublishedAt = DateTime.Now
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Health article published successfully!";
            return RedirectToAction("News");
        }
    }
}
