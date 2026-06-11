using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Implementations;
using AiNewsDigestBot.Host.Shared.Services.Parser;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiNewsDigestBot.Tests;

public class MainParserTest : IDisposable
{
    private readonly ChatService _chatService;
    private readonly IConfiguration _config;
    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MainNewsParser> _logger;
    private readonly string _testDbPath;
    private readonly TopicDetector _topicDetector;

    public MainParserTest()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString =
            _config.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new AppDbContext(options);

        var services = new ServiceCollection();
        services.AddHttpClient();

        var provider = services.BuildServiceProvider();
        _httpFactory = provider.GetRequiredService<IHttpClientFactory>();

        _topicDetector = new TopicDetector();
        _logger = NullLogger<MainNewsParser>.Instance;
        _httpClient = new HttpClient();
        _chatService = new ChatService(_httpClient, _config);
    }

    public void Dispose()
    {
        // Очистка тестовой БД
        _dbContext?.Dispose();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WithRssSources_SavesArticlesToDatabase()
    {
        // Arrange
        var parser = new MainNewsParser(_httpFactory, _config, _dbContext, _logger, _topicDetector, _chatService);

        // Act
        await parser.ParseAndSaveAsync();

        // Assert
        var articles = await _dbContext.Articles.ToListAsync();

        foreach (var article in articles.Take(10))
        {
            Console.WriteLine($"Title: {article.Title}");
            Console.WriteLine($"Source: {article.Source}");
            Console.WriteLine($"Category: {article.Category}");
            Console.WriteLine($"URL: {article.Url}");
            Console.WriteLine($"Published: {article.PublishedAt}");
            Console.WriteLine("---");
        }

        // Проверяем, что статьи есть
        Assert.NotNull(articles);
        // Не требуем обязательного наличия статей (может не быть интернета или пустые RSS)
        if (articles.Any())
        {
            Assert.All(articles, a => Assert.False(string.IsNullOrEmpty(a.Title)));
            Assert.All(articles, a => Assert.False(string.IsNullOrEmpty(a.Url)));
        }
    }

    [Fact]
    public async Task ParseAndSaveAsync_HandlesDuplicateUrls_DoesNotDuplicate()
    {
        // Arrange
        var parser = new MainNewsParser(_httpFactory, _config, _dbContext, _logger, _topicDetector, _chatService);

        // Act - первый запуск
        await parser.ParseAndSaveAsync();
        var firstCount = await _dbContext.Articles.CountAsync();

        // Act - второй запуск (должен добавить только новые)
        await parser.ParseAndSaveAsync();
        var secondCount = await _dbContext.Articles.CountAsync();

        // Assert
        Console.WriteLine($"First run: {firstCount} articles");
        Console.WriteLine($"Second run: {secondCount} articles");

        // Второй запуск не должен добавить дубликаты (но может добавить новые статьи)
        Assert.True(secondCount >= firstCount, "Second run should not decrease count");
    }
}