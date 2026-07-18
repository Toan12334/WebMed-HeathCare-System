using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Users", "Admin");
                }
                else if (User.IsInRole("Doctor"))
                {
                    return RedirectToAction("Index", "Appointment");
                }
                else if (User.IsInRole("Pharmacist"))
                {
                    return RedirectToAction("Index", "Pharmacist");
                }
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
