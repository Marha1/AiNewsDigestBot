using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AiNewsDigestBot.Tests;

/// <summary>
/// Тесты для ArticleService: дедупликация, сохранение, поиск, фильтрация
/// </summary>
public class ArticleServiceTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private ArticleService CreateService(AppDbContext db)
    {
        var logger = new Mock<ILogger<ArticleService>>().Object;
        return new ArticleService(db, logger);
    }

    private Article CreateTestArticle(string url, string title = "Test", string category = "General")
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            Url = url,
            Title = title,
            Description = "Test description",
            Category = category,
            PublishedAt = DateTime.UtcNow,
            ParsedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Проверка: FilterNewArticlesAsync возвращает только новые статьи (которых нет в БД)
    /// </summary>
    [Fact]
    public async Task FilterNewArticlesAsync_ShouldReturnOnlyNewArticles()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Существующая статья в БД
        var existingArticle = CreateTestArticle("https://example.com/1");
        db.Articles.Add(existingArticle);
        await db.SaveChangesAsync();

        var articles = new List<Article>
        {
            CreateTestArticle("https://example.com/1"), // дубликат
            CreateTestArticle("https://example.com/2"), // новая
            CreateTestArticle("https://example.com/3")  // новая
        };

        var result = await service.FilterNewArticlesAsync(articles);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Url == "https://example.com/1");
        Assert.Contains(result, a => a.Url == "https://example.com/2");
        Assert.Contains(result, a => a.Url == "https://example.com/3");
    }

    /// <summary>
    /// Проверка: FilterNewArticlesAsync удаляет дубликаты внутри одного списка
    /// </summary>
    [Fact]
    public async Task FilterNewArticlesAsync_ShouldRemoveDuplicatesWithinList()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var articles = new List<Article>
        {
            CreateTestArticle("https://example.com/1"),
            CreateTestArticle("https://example.com/1"), // дубликат
            CreateTestArticle("https://example.com/2")
        };

        var result = await service.FilterNewArticlesAsync(articles);

        Assert.Equal(2, result.Count);
        Assert.Single(result.Where(a => a.Url == "https://example.com/1"));
    }

    /// <summary>
    /// Проверка: SaveArticlesAsync сохраняет только новые статьи (пропускает дубли)
    /// </summary>
    [Fact]
    public async Task SaveArticlesAsync_ShouldSaveOnlyNewArticles()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var existingArticle = CreateTestArticle("https://example.com/1");
        db.Articles.Add(existingArticle);
        await db.SaveChangesAsync();

        var articles = new List<Article>
        {
            CreateTestArticle("https://example.com/1"), // дубликат
            CreateTestArticle("https://example.com/2"), // новая
            CreateTestArticle("https://example.com/3")  // новая
        };

        var savedCount = await service.SaveArticlesAsync(articles);

        Assert.Equal(2, savedCount);
        var allArticles = await db.Articles.ToListAsync();
        Assert.Equal(3, allArticles.Count);
    }

    /// <summary>
    /// Проверка: GetArticlesByTopicsAsync возвращает статьи только по указанным темам
    /// </summary>
    [Fact]
    public async Task GetArticlesByTopicsAsync_ShouldReturnArticlesForSpecifiedTopics()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var articles = new List<Article>
        {
            CreateTestArticle("https://example.com/1", "Tech 1", "Technology"),
            CreateTestArticle("https://example.com/2", "AI 1", "ArtificialIntelligence"),
            CreateTestArticle("https://example.com/3", "Sports 1", "Sports"),
            CreateTestArticle("https://example.com/4", "Tech 2", "Technology")
        };
        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();

        var topics = new List<string> { "Technology", "Sports" };

        var result = await service.GetArticlesByTopicsAsync(topics);

        Assert.Equal(3, result.Count);
        Assert.All(result, a => Assert.Contains(a.Category, topics));
    }

    /// <summary>
    /// Проверка: SearchArticlesAsync находит статьи по тексту в заголовке
    /// </summary>
    [Fact]
    public async Task SearchArticlesAsync_ShouldReturnArticlesMatchingQuery()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var articles = new List<Article>
        {
            CreateTestArticle("https://example.com/1", "AI breakthrough in medicine"),
            CreateTestArticle("https://example.com/2", "New technology for cars"),
            CreateTestArticle("https://example.com/3", "Sports results today")
        };
        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();

        var result = await service.SearchArticlesAsync("AI");

        Assert.Single(result);
        Assert.Contains("AI", result[0].Title);
    }

    /// <summary>
    /// Проверка: GetLatestArticlesAsync возвращает статьи, отсортированные по дате (сначала новые)
    /// </summary>
    [Fact]
    public async Task GetLatestArticlesAsync_ShouldReturnArticlesOrderedByDate()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var articles = new List<Article>();
        for (int i = 1; i <= 5; i++)
        {
            var article = CreateTestArticle($"https://example.com/{i}", $"Article {i}");
            article.PublishedAt = DateTime.UtcNow.AddDays(-i);
            articles.Add(article);
        }
        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();

        var result = await service.GetLatestArticlesAsync(3);

        Assert.Equal(3, result.Count);
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].PublishedAt >= result[i + 1].PublishedAt);
        }
    }
}