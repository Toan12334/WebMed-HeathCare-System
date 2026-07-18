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
                .Where(u => u.AccountStatus != "SoftDeleted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail) || u.FullName.Contains(searchEmail));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.SearchEmail = searchEmail;
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(users);
        }

        // GET: /Admin/GetUserDetails/5
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return Json(new
            {
                userId = user.UserId,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                roleId = user.RoleId,
                roleName = user.Role.RoleName,
                isActive = user.IsActive,
                accountStatus = user.AccountStatus,
                doctor = user.Doctor != null ? new
                {
                    specialty = user.Doctor.Specialty,
                    location = user.Doctor.Location,
                    bio = user.Doctor.Bio
                } : null,
                patient = user.Patient != null ? new
                {
                    dob = user.Patient.DateOfBirth?.ToString("yyyy-MM-dd"),
                    gender = user.Patient.Gender,
                    address = user.Patient.Address,
                    bloodType = user.Patient.BloodType
                } : null
            });
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(string username, string password, string fullName, string email, string phoneNumber, int roleId, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower()))
            {
                TempData["ErrorMessage"] = "Username or Email already exists.";
                return RedirectToAction("Users");
            }

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Invalid Role selected.";
                return RedirectToAction("Users");
            }

            var user = new User
            {
                Username = username,
                PasswordHash = password,
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                RoleId = roleId,
                AccountStatus = "Active",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (role.RoleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                var doctor = new Doctor
                {
                    DoctorId = user.UserId,
                    Specialty = specialty ?? "General Practitioner",
                    Location = location ?? "Clinic",
                    Bio = bio ?? "",
                    AverageRating = 0,
                    IsVerified = true,
                    IsActive = true
                };
                _context.Doctors.Add(doctor);
            }
            else if (role.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            {
                var patient = new Patient
                {
                    PatientId = user.UserId,
                    DateOfBirth = dob,
                    Gender = gender,
                    Address = address,
                    BloodType = bloodType,
                    IsActive = true
                };
                _context.Patients.Add(patient);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{fullName}' created successfully as '{role.RoleName}'.";
            return RedirectToAction("Users");
        }

        // POST: /Admin/EditUser
        [HttpPost]
        public async Task<IActionResult> EditUser(int userId, string fullName, string email, string phoneNumber, int roleId, string password, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            if (user.Email.ToLower() != email.ToLower())
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.UserId != userId))
                {
                    TempData["ErrorMessage"] = "Email is already in use by another account.";
                    return RedirectToAction("Users");
                }
            }

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.RoleId = roleId;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(password))
            {
                user.PasswordHash = password;
            }

            await _context.SaveChangesAsync();

            var role = await _context.Roles.FindAsync(roleId);
            if (role != null)
            {
                if (role.RoleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                {
                    if (user.Patient != null) _context.Patients.Remove(user.Patient);

                    if (user.Doctor == null)
                    {
                        var doctor = new Doctor
                        {
                            DoctorId = user.UserId,
                            Specialty = specialty ?? "General Practitioner",
                            Location = location ?? "Clinic",
                            Bio = bio ?? "",
                            AverageRating = 0,
                            IsVerified = true,
                            IsActive = true
                        };
                        _context.Doctors.Add(doctor);
                    }
                    else
                    {
                        user.Doctor.Specialty = specialty ?? "General Practitioner";
                        user.Doctor.Location = location ?? "Clinic";
                        user.Doctor.Bio = bio ?? "";
                    }
                }
                else if (role.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    if (user.Doctor != null) _context.Doctors.Remove(user.Doctor);

                    if (user.Patient == null)
                    {
                        var patient = new Patient
                        {
                            PatientId = user.UserId,
                            DateOfBirth = dob,
                            Gender = gender,
                            Address = address,
                            BloodType = bloodType,
                            IsActive = true
                        };
                        _context.Patients.Add(patient);
                    }
                    else
                    {
                        user.Patient.DateOfBirth = dob;
                        user.Patient.Gender = gender;
                        user.Patient.Address = address;
                        user.Patient.BloodType = bloodType;
                    }
                }
                else
                {
                    if (user.Doctor != null) _context.Doctors.Remove(user.Doctor);
                    if (user.Patient != null) _context.Patients.Remove(user.Patient);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{fullName}' updated successfully.";
            return RedirectToAction("Users");
        }

        // POST: /Admin/SuspendUser
        [HttpPost]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.FullName}' account active status changed to {user.IsActive}.";
            return RedirectToAction("Users");
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            user.AccountStatus = "SoftDeleted";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.FullName}' account has been deleted.";
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
        public async Task<IActionResult> CreateNews(string title, string category, string content, string imageUrl)
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
                PublishedAt = DateTime.Now,
                ImageUrl = imageUrl
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Health article published successfully!";
            return RedirectToAction("News");
        }

        // ==========================================
        // UC 29: MANAGE SYSTEM ROLES & PERMISSIONS
        // ==========================================

        // GET: /Admin/Roles
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        // POST: /Admin/CreateRole
        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName, string description)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["ErrorMessage"] = "Role name is required.";
                return RedirectToAction("Roles");
            }

            if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower()))
            {
                TempData["ErrorMessage"] = $"Role '{roleName}' already exists.";
                return RedirectToAction("Roles");
            }

            var role = new Role
            {
                RoleName = roleName,
                Description = description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Role '{roleName}' created successfully.";
            return RedirectToAction("Roles");
        }

        // POST: /Admin/EditRole
        [HttpPost]
        public async Task<IActionResult> EditRole(int roleId, string roleName, string description)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return NotFound();

            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["ErrorMessage"] = "Role name is required.";
                return RedirectToAction("Roles");
            }

            if (role.RoleName.ToLower() != roleName.ToLower())
            {
                if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower() && r.RoleId != roleId))
                {
                    TempData["ErrorMessage"] = $"Role name '{roleName}' is already taken.";
                    return RedirectToAction("Roles");
                }
            }

            role.RoleName = roleName;
            role.Description = description;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Role '{roleName}' updated successfully.";
            return RedirectToAction("Roles");
        }

        // POST: /Admin/DeleteRole
        [HttpPost]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            // Verify if any active/inactive users are assigned to this role
            var hasUsers = await _context.Users.AnyAsync(u => u.RoleId == id && u.AccountStatus != "SoftDeleted");
            if (hasUsers)
            {
                TempData["ErrorMessage"] = $"Cannot delete role '{role.RoleName}' because it is currently assigned to one or more user accounts.";
                return RedirectToAction("Roles");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Role '{role.RoleName}' deleted successfully.";
            return RedirectToAction("Roles");
        }
    }
}
