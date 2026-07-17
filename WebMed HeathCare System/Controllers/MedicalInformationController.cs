using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class MedicalInformationController : Controller
    {
        private readonly WebMedDbContext _context;

        public MedicalInformationController(WebMedDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(string keyword)
        {
            ViewBag.Keyword = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                ViewBag.Message = "Please enter a keyword to search.";
                return View();
            }

            var results = await _context.Articles
                .Where(a => (a.Title.Contains(keyword) || a.Content.Contains(keyword)) && a.IsActive && a.IsPublished)
                .ToListAsync();

            if (results.Count == 0)
            {
                ViewBag.Message = "No information found.";
            }

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author) // Include author if needed
                .FirstOrDefaultAsync(a => a.ArticleId == id);

            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }
    }
}
