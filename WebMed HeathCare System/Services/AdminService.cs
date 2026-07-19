using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class AdminService : IAdminService
    {
        private readonly WebMedDbContext _context;

        public AdminService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetUsersAsync(string? searchEmail)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Where(u => u.AccountStatus != "SoftDeleted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail) || u.FullName.Contains(searchEmail));
            }

            return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<object?> GetUserDetailsAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return null;

            return new
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
            };
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(string username, string password, string fullName, string email, string phoneNumber, int roleId, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower()))
            {
                return (false, "Username or Email already exists.");
            }

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                return (false, "Invalid Role selected.");
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
                _context.Doctors.Add(new Doctor
                {
                    DoctorId = user.UserId,
                    Specialty = specialty ?? "General Practitioner",
                    Location = location ?? "Clinic",
                    Bio = bio ?? "",
                    AverageRating = 0,
                    IsVerified = true,
                    IsActive = true
                });
            }
            else if (role.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            {
                _context.Patients.Add(new Patient
                {
                    PatientId = user.UserId,
                    DateOfBirth = dob,
                    Gender = gender,
                    Address = address,
                    BloodType = bloodType,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
            return (true, $"User '{fullName}' created successfully as '{role.RoleName}'.");
        }

        public async Task<(bool Success, string Message)> EditUserAsync(int userId, string fullName, string email, string phoneNumber, int roleId, string password, string specialty, string location, string bio, string gender, string address, string bloodType, DateTime? dob)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.Email.ToLower() != email.ToLower() &&
                await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.UserId != userId))
            {
                return (false, "Email is already in use by another account.");
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
                        _context.Doctors.Add(new Doctor
                        {
                            DoctorId = user.UserId,
                            Specialty = specialty ?? "General Practitioner",
                            Location = location ?? "Clinic",
                            Bio = bio ?? "",
                            AverageRating = 0,
                            IsVerified = true,
                            IsActive = true
                        });
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
                        _context.Patients.Add(new Patient
                        {
                            PatientId = user.UserId,
                            DateOfBirth = dob,
                            Gender = gender,
                            Address = address,
                            BloodType = bloodType,
                            IsActive = true
                        });
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
            return (true, $"User '{fullName}' updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleUserActiveAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return (false, "User not found.");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return (true, $"User '{user.FullName}' account active status changed to {user.IsActive}.");
        }

        public async Task<(bool Success, string Message)> SoftDeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return (false, "User not found.");

            user.IsActive = false;
            user.AccountStatus = "SoftDeleted";
            await _context.SaveChangesAsync();

            return (true, $"User '{user.FullName}' account has been deleted.");
        }

        public async Task<List<DoctorLicense>> GetPendingDoctorLicensesAsync()
        {
            return await _context.DoctorLicenses
                .Include(l => l.Doctor)
                .ThenInclude(d => d.DoctorNavigation)
                .Where(l => l.VerificationStatus == "Pending")
                .ToListAsync();
        }

        public async Task<bool> VerifyDoctorAsync(int licenseId, string status, int? adminId)
        {
            var license = await _context.DoctorLicenses.FindAsync(licenseId);
            if (license == null) return false;

            if (adminId.HasValue)
            {
                license.ReviewedBy = adminId.Value;
            }

            license.VerificationStatus = status;
            license.ReviewedAt = DateTime.Now;

            var doctor = await _context.Doctors.FindAsync(license.DoctorId);
            if (doctor != null)
            {
                doctor.IsVerified = status == "Approved";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DoctorReview>> GetReviewsAsync()
        {
            return await _context.DoctorReviews
                .Include(r => r.Patient).ThenInclude(p => p.PatientNavigation)
                .Include(r => r.Doctor).ThenInclude(d => d.DoctorNavigation)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ModerateReviewAsync(int id, string status)
        {
            var review = await _context.DoctorReviews.FindAsync(id);
            if (review == null) return false;

            review.ModerationStatus = status;
            await _context.SaveChangesAsync();

            var doctor = await _context.Doctors.FindAsync(review.DoctorId);
            if (doctor != null)
            {
                var ratings = await _context.DoctorReviews
                    .Where(r => r.DoctorId == doctor.DoctorId && r.ModerationStatus == "Approved")
                    .Select(r => r.Rating)
                    .ToListAsync();

                doctor.AverageRating = ratings.Any() ? (decimal)ratings.Average() : 0;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<List<Article>> GetNewsAsync()
        {
            return await _context.Articles
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();
        }

        public async Task<bool> CreateNewsAsync(string title, string? category, string content, string? imageUrl, int authorId)
        {
            _context.Articles.Add(new Article
            {
                Title = title,
                Category = category ?? "General",
                Content = content,
                AuthorId = authorId,
                IsPublished = true,
                IsActive = true,
                PublishedAt = DateTime.Now,
                ImageUrl = imageUrl
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> CreateRoleAsync(string roleName, string description)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return (false, "Role name is required.");
            }

            if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower()))
            {
                return (false, $"Role '{roleName}' already exists.");
            }

            _context.Roles.Add(new Role { RoleName = roleName, Description = description });
            await _context.SaveChangesAsync();

            return (true, $"Role '{roleName}' created successfully.");
        }

        public async Task<(bool Success, string Message)> EditRoleAsync(int roleId, string roleName, string description)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return (false, "Role not found.");

            var systemRoles = new HashSet<string> { "Admin", "Doctor", "Pharmacist", "Patient" };
            var isSystem = systemRoles.Contains(role.RoleName);

            if (isSystem)
            {
                roleName = role.RoleName;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return (false, "Role name is required.");
                }

                if (role.RoleName.ToLower() != roleName.ToLower() &&
                    await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower() && r.RoleId != roleId))
                {
                    return (false, $"Role name '{roleName}' is already taken.");
                }

                role.RoleName = roleName;
            }

            role.Description = description;
            await _context.SaveChangesAsync();

            return (true, $"Role '{roleName}' updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return (false, "Role not found.");

            var systemRoles = new HashSet<string> { "Admin", "Doctor", "Pharmacist", "Patient" };
            if (systemRoles.Contains(role.RoleName))
            {
                return (false, $"Cannot delete system core role '{role.RoleName}'.");
            }

            var hasUsers = await _context.Users.AnyAsync(u => u.RoleId == id && u.AccountStatus != "SoftDeleted");
            if (hasUsers)
            {
                return (false, $"Cannot delete role '{role.RoleName}' because it is currently assigned to one or more user accounts.");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return (true, $"Role '{role.RoleName}' deleted successfully.");
        }

        public async Task<List<AmbulanceRequest>> GetEmergencyRequestsAsync()
        {
            return await _context.AmbulanceRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<bool> DispatchAmbulanceAsync(int requestId, string? vehicleNumber)
        {
            var request = await _context.AmbulanceRequests.FindAsync(requestId);
            if (request == null) return false;

            if (string.IsNullOrWhiteSpace(vehicleNumber))
            {
                vehicleNumber = "AMB-1024";
            }

            request.Status = "Assigned";
            request.AssignedAmbulanceVehicle = vehicleNumber;

            decimal destLat = request.Latitude ?? 10.776m;
            decimal destLng = request.Longitude ?? 106.700m;

            request.AmbulanceLatitude = destLat - 0.008m;
            request.AmbulanceLongitude = destLng - 0.008m;
            request.Eta = "15 mins";
            request.RequestedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
