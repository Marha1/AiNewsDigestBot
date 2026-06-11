using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class ArticleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(AppDbContext db, ILogger<ArticleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Дедупликация: возвращает только новые статьи (которых нет в БД)
    /// </summary>
    public async Task<List<Article>> FilterNewArticlesAsync(List<Article> articles)
    {
        if (articles == null || articles.Count == 0)
            return new List<Article>();
        
        var urls = articles.Select(a => a.Url).ToList();
        var existingUrls = await _db.Articles
            .Where(a => urls.Contains(a.Url))
            .Select(a => a.Url)
            .ToHashSetAsync();
        
        return articles.Where(a => !existingUrls.Contains(a.Url)).ToList();
    }

    /// <summary>
    /// Сохраняет статьи в БД
    /// </summary>
    public async Task<int> SaveArticlesAsync(List<Article> articles)
    {
        if (articles == null || articles.Count == 0)
            return 0;
        
        await _db.Articles.AddRangeAsync(articles);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Saved {Count} new articles", articles.Count);
        return articles.Count;
    }

    /// <summary>
    /// Поиск статей по категориям (для дайджеста)
    /// </summary>
    public async Task<List<Article>> GetArticlesByCategoriesAsync(List<string> categories, int limit = 10)
    {
        return await _db.Articles
            .Where(a => categories.Contains(a.Category))
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Последние статьи без фильтра
    /// </summary>
    public async Task<List<Article>> GetLatestArticlesAsync(int limit = 10)
    {
        return await _db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Поиск по заголовку
    /// </summary>
    public async Task<List<Article>> SearchArticlesAsync(string query, int limit = 10)
    {
        return await _db.Articles
            .Where(a => a.Title.Contains(query) || (a.Description != null && a.Description.Contains(query)))
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Проверка существования статьи по URL
    /// </summary>
    public async Task<bool> ArticleExistsAsync(string url)
    {
        return await _db.Articles.AnyAsync(a => a.Url == url);
    }
    
}