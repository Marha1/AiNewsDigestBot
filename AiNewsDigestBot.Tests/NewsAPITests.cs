/*// tests/AiNewsDigestBot.Tests/Parsers/NewsApiParserTests.cs
using System.Text.Json;
using AiNewsDigestBot.Host.Shared.Services.Parser;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AiNewsDigestBot.Tests.Parsers;

/// <summary>
/// Тесты для NewsAPI с реальным API
/// </summary>
public class NewsApiParserTests
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public NewsApiParserTests()
    {
        // Загружаем конфигурацию с API ключом
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task NewsApiParser_TopHeadlinesTechnology_ReturnsArticles()
    {
        // Arrange
        var parser = new NewsParser(_httpClient, _config);
        var url = $"https://newsapi.org/v2/top-headlines?" + "sources=bbc-news";

        // Act
        var articles = await parser.ParseAsync(url, "NewsAPI", "Technology");

        // Assert
        Assert.NotNull(articles);
        Assert.NotEmpty(articles);

        // Выводим результаты
        Console.WriteLine($"\n=== NewsAPI: Top Headlines Technology ===");
        Console.WriteLine($"Found {articles.Count} articles\n");

        foreach (var article in articles.Take(5))
        {
            Console.WriteLine($"Title: {article.Title}");
            Console.WriteLine($"Published: {article.PublishedAt}");
            Console.WriteLine($"URL: {article.Url}");
            Console.WriteLine($"Category: {article.Category}");
            Console.WriteLine("---");
        }
    }

    [Fact]
    public async Task NewsApiParser_EverythingAI_ReturnsArticles()
    {
        // Arrange
        var parser = new NewsParser(_httpClient, _config);
        var url = "https://newsapi.org/v2/everything?q=artificial+intelligence&language=en&sortBy=publishedAt";

        // Act
        var articles = await parser.ParseAsync(url, "NewsAPI", "ArtificialIntelligence");

        // Assert
        Assert.NotNull(articles);
        Assert.NotEmpty(articles);

        // Выводим результаты
        Console.WriteLine($"\n=== NewsAPI: Everything AI ===");
        Console.WriteLine($"Found {articles.Count} articles\n");

        foreach (var article in articles.Take(5))
        {
            Console.WriteLine($"Title: {article.Title}");
            Console.WriteLine($"Published: {article.PublishedAt}");
            Console.WriteLine($"URL: {article.Url}");
            Console.WriteLine($"Category: {article.Category}");
            Console.WriteLine("---");
        }
    }

    [Fact]
    public async Task NewsApiParser_MultipleCategories_AllWork()
    {
        // Arrange
        var parser = new NewsParser(_httpClient, _config);

        var categories = new[]
        {
            ("business", "Business"),
            ("science", "Science"),
            ("health", "Health"),
            ("sports", "Sports"),
            ("entertainment", "Entertainment")
        };

        foreach (var (category, topic) in categories)
        {
            // Act
            var url = $"https://newsapi.org/v2/top-headlines?country=us&category={category}";
            var articles = await parser.ParseAsync(url, "NewsAPI", topic);

            // Assert
            Assert.NotNull(articles);
            Console.WriteLine($"\n=== NewsAPI: {topic} ===");
            Console.WriteLine($"Found {articles.Count} articles");

            if (articles.Any())
            {
                var first = articles.First();
                Console.WriteLine($"Example: {first.Title}");
            }

            // Не перегружаем API
            await Task.Delay(1000);
        }
    }

    [Fact]
    public async Task NewsApiParser_InvalidApiKey_ReturnsEmptyList()
    {
        // Arrange
        var badConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NewsApi:Key"] = "invalid-key-12345"
            })
            .Build();

        var parser = new NewsParser(_httpClient, badConfig);
        var url = "https://newsapi.org/v2/top-headlines?country=us";

        // Act
        var articles = await parser.ParseAsync(url, "NewsAPI", "Technology");

        // Assert
        Assert.NotNull(articles);
        Assert.Empty(articles);
        Console.WriteLine("\n=== NewsAPI: Invalid API Key Test ===");
        Console.WriteLine("Correctly returned empty list for invalid key");
    }
}*/

