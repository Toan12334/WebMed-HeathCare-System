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
        public async Task<IActionResult> Search(string category, string keyword)
        {
            ViewBag.SelectedCategory = category;
            ViewBag.Keyword = keyword;

            var query = _context.Articles
                .Where(a => a.IsActive && a.IsPublished);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(a => a.Title.Contains(keyword) || a.Content.Contains(keyword));
            }

            var articles = await query
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            ViewBag.Categories = new List<string>
            {
                "News",
                "Notifications",
                "Hospital Activities",
                "Research",
                "Events",
                "Cooperation",
                "Training",
                "Community",
                "Pharma Info",
                "Recruitment",
                "Success Stories"
            };

            return View(articles);
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

            // Get related articles (same category, excluding current article, limit to 4)
            var relatedArticles = await _context.Articles
                .Where(a => a.Category == article.Category && a.ArticleId != id && a.IsActive && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Take(4)
                .ToListAsync();

            // If not enough related articles, backfill with other latest articles
            if (relatedArticles.Count < 4)
            {
                var needed = 4 - relatedArticles.Count;
                var backfillIds = relatedArticles.Select(ra => ra.ArticleId).Concat(new[] { id }).ToList();
                var backfill = await _context.Articles
                    .Where(a => !backfillIds.Contains(a.ArticleId) && a.IsActive && a.IsPublished)
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(needed)
                    .ToListAsync();
                relatedArticles.AddRange(backfill);
            }

            ViewBag.RelatedArticles = relatedArticles;

            return View(article);
        }
    }
}
