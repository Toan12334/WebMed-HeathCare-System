using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class UserService : IUserService
    {
        private readonly WebMedDbContext _context;

        public UserService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            // Query user and validate password
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.PasswordHash != password)
            {
                return null; // Authentication failed
            }

            return user; // Authentication successful
        }

        public async Task<bool> CheckUserExistsAsync(string username, string email)
        {
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> RegisterPatientAsync(RegisterViewModel model)
        {
            // Start transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Double check if account exists
                if (await CheckUserExistsAsync(model.Username, model.Email))
                {
                    return false;
                }

                // Get Patient role
                var patientRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "patient");
                
                if (patientRole == null)
                {
                    // Fallback to first role or create default one
                    patientRole = await _context.Roles.FirstOrDefaultAsync();
                    if (patientRole == null)
                    {
                        patientRole = new Role { RoleName = "Patient", Description = "Patient Role" };
                        _context.Roles.Add(patientRole);
                        await _context.SaveChangesAsync();
                    }
                }

                // Create User
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password, // Typically hashed, keeping matched representation here
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    RoleId = patientRole.RoleId,
                    AccountStatus = "Active",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // Generates UserId

                // Create Patient
                var patient = new Patient
                {
                    PatientId = user.UserId, // One-to-one relationship
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Address = model.Address,
                    BloodType = model.BloodType,
                    IsActive = true
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true; // Registration successful
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false; // Registration failed
            }
        }
    }
}
