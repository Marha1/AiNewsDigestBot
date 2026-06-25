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
    ///     Дедупликация: возвращает только новые статьи (которых нет в БД)
    /// </summary>
    public async Task<List<Article>> FilterNewArticlesAsync(List<Article> articles)
    {
        if (articles == null || articles.Count == 0)
            return new List<Article>();

        // 1. Убираем дубли внутри списка
        var uniqueArticles = articles
            .GroupBy(a => a.Url)
            .Select(g => g.First())
            .ToList();

        // 2. Проверяем, какие URL уже есть в БД
        var urls = uniqueArticles.Select(a => a.Url).ToList();
        var existingUrls = await _db.Articles
            .Where(a => urls.Contains(a.Url))
            .Select(a => a.Url)
            .ToHashSetAsync();

        // 3. Возвращаем только новые статьи
        return uniqueArticles.Where(a => !existingUrls.Contains(a.Url)).ToList();
    }

    /// <summary>
    ///     Сохраняет статьи в БД
    /// </summary>
    public async Task<int> SaveArticlesAsync(List<Article> articles)
    {
        if (articles == null || articles.Count == 0)
            return 0;

        // 1. Убираем дубли по URL внутри списка
        var uniqueArticles = articles
            .GroupBy(a => a.Url)
            .Select(g => g.First())
            .ToList();

        if (uniqueArticles.Count < articles.Count)
            _logger.LogWarning("Removed {Duplicates} duplicate URLs from the batch",
                articles.Count - uniqueArticles.Count);

        // 2. Проверяем, какие URL уже есть в БД
        var urls = uniqueArticles.Select(a => a.Url).ToList();
        var existingUrls = await _db.Articles
            .Where(a => urls.Contains(a.Url))
            .Select(a => a.Url)
            .ToHashSetAsync();

        // 3. Фильтруем только новые статьи
        var newArticles = uniqueArticles
            .Where(a => !existingUrls.Contains(a.Url))
            .ToList();

        if (newArticles.Count == 0)
        {
            _logger.LogInformation("All {Count} articles already exist in database", articles.Count);
            return 0;
        }

        // 4. Сохраняем только новые статьи
        await _db.Articles.AddRangeAsync(newArticles);
        await _db.SaveChangesAsync();

        var skipped = articles.Count - newArticles.Count;
        _logger.LogInformation("Saved {Saved} new articles (skipped {Skipped} duplicates)",
            newArticles.Count, skipped);

        return newArticles.Count;
    }

    /// <summary>
    ///     Получить статьи по темам (для дайджеста)
    /// </summary>
    public async Task<List<Article>> GetArticlesByTopicsAsync(List<string> topics, int limit = 10)
    {
        if (topics == null || topics.Count == 0)
            return new List<Article>();

        return await _db.Articles
            .Where(a => topics.Contains(a.Category))
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    ///     Получить статьи по темам с пагинацией
    /// </summary>
    public async Task<(List<Article> Articles, int TotalCount)> GetArticlesByTopicsPagedAsync(
        List<string> topics,
        int page = 1,
        int pageSize = 10)
    {
        if (topics == null || topics.Count == 0)
            return (new List<Article>(), 0);

        var query = _db.Articles
            .Where(a => topics.Contains(a.Category))
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var articles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (articles, totalCount);
    }
    

    /// <summary>
    ///     Последние статьи без фильтра
    /// </summary>
    public async Task<List<Article>> GetLatestArticlesAsync(int limit = 10)
    {
        return await _db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    ///     Последние статьи с пагинацией
    /// </summary>
    public async Task<List<Article>> GetLatestArticlesPagedAsync(int page, int pageSize)
    {
        return await _db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    ///     Поиск статей с пагинацией
    /// </summary>
    public async Task<List<Article>> SearchArticlesPagedAsync(string query, int page, int pageSize)
    {
        return await _db.Articles
            .Where(a =>
                a.Title.Contains(query) ||
                (a.Description != null && a.Description.Contains(query)))
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    ///     Поиск по заголовку, описанию и суммаризации
    /// </summary>
    public async Task<List<Article>> SearchArticlesAsync(string query, int limit = 10)
    {
        return await _db.Articles
            .Where(a => a.Title.Contains(query) ||
                        (a.Description != null && a.Description.Contains(query)) ||
                        (a.Summary != null && a.Summary.Contains(query)))
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }
    
}