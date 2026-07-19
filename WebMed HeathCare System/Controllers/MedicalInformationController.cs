using Microsoft.AspNetCore.Mvc;
using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Controllers
{
    public class MedicalInformationController : Controller
    {
        private readonly IMedicalInformationService _medicalInformationService;

        public MedicalInformationController(IMedicalInformationService medicalInformationService)
        {
            _medicalInformationService = medicalInformationService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string category, string keyword)
        {
            ViewBag.SelectedCategory = category;
            ViewBag.Keyword = keyword;

            var articles = await _medicalInformationService.SearchArticlesAsync(category, keyword);

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
            var article = await _medicalInformationService.GetArticleByIdAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            var relatedArticles = await _medicalInformationService.GetRelatedArticlesAsync(article, 4);

            ViewBag.RelatedArticles = relatedArticles;

            return View(article);
        }
    }
}
