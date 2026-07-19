using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IMedicalInformationService
    {
        Task<List<Article>> SearchArticlesAsync(string? category, string? keyword);
        Task<Article?> GetArticleByIdAsync(int id);
        Task<List<Article>> GetRelatedArticlesAsync(Article article, int count);
    }
}
