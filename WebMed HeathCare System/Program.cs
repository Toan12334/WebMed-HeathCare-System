using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Services;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure DbContext with Connection String from appsettings.json
            builder.Services.AddDbContext<WebMedDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register Service Layer
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IEmergencyRequestService, EmergencyRequestService>();
            builder.Services.AddScoped<WebMed_HeathCare_System.Interfaces.IHealthCalculatorService, WebMed_HeathCare_System.Services.HealthCalculatorService>();
            
            // Register Insurance Guide Repositories
            builder.Services.AddScoped<IInsurancePlanRepository, InsurancePlanRepository>();
            builder.Services.AddScoped<ICoverageRepository, CoverageRepository>();
            builder.Services.AddScoped<IPricingRepository, PricingRepository>();
            // Configure Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Authentication/Login";
                    options.LogoutPath = "/Authentication/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
