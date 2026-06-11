using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public class MainNewsParser
{
    private const int DelayForNewsApiMs = 1000;
    private readonly IChatService _chatService;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MainNewsParser> _logger;
    private readonly TopicDetector _topicDetector;

    public MainNewsParser(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        AppDbContext db,
        ILogger<MainNewsParser> logger,
        TopicDetector topicDetector,
        IChatService chatService)
    {
        _httpFactory = httpFactory;
        _config = config;
        _db = db;
        _logger = logger;
        _topicDetector = topicDetector;
        _chatService = chatService;
    }

    public async Task ParseAndSaveAsync()
    {
        var httpClient = _httpFactory.CreateClient();
        var parser = new NewsParser(httpClient, _config);

        var sources = await _db.Sources.ToListAsync();

        if (sources.Count == 0)
        {
            _logger.LogWarning("No active sources found.");
            return;
        }

        foreach (var source in sources)
            try
            {
                var articles = await parser.ParseAsync(source.Url);
                if (articles == null || articles.Count == 0)
                    continue;

                // ===== 1. Пакетная дедупликация =====
                var urls = articles.Select(a => a.Url).ToList();
                var existingUrls = await _db.Articles
                    .Where(a => urls.Contains(a.Url))
                    .Select(a => a.Url)
                    .ToHashSetAsync();

                var newArticles = articles.Where(a => !existingUrls.Contains(a.Url)).ToList();
                if (newArticles.Count == 0)
                {
                    _logger.LogInformation("No new articles from {Name}");
                    continue;
                }

                // ток для новых
                foreach (var article in newArticles)
                {
                    var detectedCategory = _topicDetector.Detect(article.Title, article.Description);
                    article.Category = detectedCategory?.ToString() ?? "General";
                    article.SourceId = source.Id;

                    var textToSummarize = string.IsNullOrWhiteSpace(article.Description)
                        ? article.Title
                        : article.Description;
                    article.Summary = await _chatService.SummarizeAsync(textToSummarize);
                }

                await _db.Articles.AddRangeAsync(newArticles);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Saved and summarized {Count} new articles from {Name}", newArticles.Count);

                if (source.Url.Contains("newsapi.org"))
                    await Task.Delay(DelayForNewsApiMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source {Name}");
            }
    }
}