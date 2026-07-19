using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class MedicalInformationService : IMedicalInformationService
    {
        private readonly WebMedDbContext _context;

        public MedicalInformationService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<Article>> SearchArticlesAsync(string? category, string? keyword)
        {
            var query = _context.Articles.Where(a => a.IsActive && a.IsPublished);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(a => a.Title.Contains(keyword) || a.Content.Contains(keyword));
            }

            return await query
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();
        }

        public async Task<Article?> GetArticleByIdAsync(int id)
        {
            return await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.ArticleId == id);
        }

        public async Task<List<Article>> GetRelatedArticlesAsync(Article article, int count)
        {
            var relatedArticles = await _context.Articles
                .Where(a => a.Category == article.Category && a.ArticleId != article.ArticleId && a.IsActive && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();

            if (relatedArticles.Count < count)
            {
                var needed = count - relatedArticles.Count;
                var backfillIds = relatedArticles.Select(ra => ra.ArticleId).Concat(new[] { article.ArticleId }).ToList();
                var backfill = await _context.Articles
                    .Where(a => !backfillIds.Contains(a.ArticleId) && a.IsActive && a.IsPublished)
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(needed)
                    .ToListAsync();

                relatedArticles.AddRange(backfill);
            }

            return relatedArticles;
        }
    }
}
