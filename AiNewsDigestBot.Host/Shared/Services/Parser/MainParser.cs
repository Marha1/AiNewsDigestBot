using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public class MainNewsParser
{
    private const int DelayForNewsApiMs = 1000;
    private const int MaxArticlesPerSource = 15;
    private const int DelayBetweenAiCallsMs = 500;
    private readonly ArticleService _articleService;
    private readonly IChatService _chatService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MainNewsParser> _logger;
    private readonly NewsParser _newsParser;
    private readonly SourceService _sourceService;
    private readonly TopicDetector _topicDetector;

    public MainNewsParser(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<MainNewsParser> logger,
        TopicDetector topicDetector,
        IChatService chatService,
        ArticleService articleService,
        SourceService sourceService, NewsParser newsParser
    )
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
        _topicDetector = topicDetector;
        _chatService = chatService;
        _articleService = articleService;
        _sourceService = sourceService;
        _newsParser = newsParser;
    }

    public async Task ParseAndSaveAsync()
    {
        var sources = await _sourceService.GetActiveSourcesAsync();
        if (sources.Count == 0)
        {
            _logger.LogWarning("No active sources found.");
            return;
        }

        foreach (var source in sources)
            try
            {
                // 1. Парсим с лимитом
                var articles = await _newsParser.ParseAsync(source.Url, MaxArticlesPerSource);
                if (articles == null || articles.Count == 0)
                    continue;

                // 2. Дедупликация
                var newArticles = await _articleService.FilterNewArticlesAsync(articles);

                var skippedCount = articles.Count - newArticles.Count;
                if (skippedCount > 0)
                    _logger.LogInformation("Skipped {SkippedCount} duplicate articles from {SourceName}",
                        skippedCount, source);

                if (newArticles.Count == 0)
                {
                    _logger.LogInformation("All {TotalCount} articles from {SourceName} are duplicates, skipping",
                        articles.Count, source);
                    continue;
                }

                // 3. Обрабатываем новые статьи
                var summarizedCount = 0;
                var failedCount = 0;

                foreach (var article in newArticles)
                    try
                    {
                        // Категория
                        var detectedCategory = _topicDetector.Detect(article.Title, article.Description);
                        article.Category = detectedCategory?.ToString() ?? "General";

                        // Связь с источником
                        article.SourceId = source.Id;

                        // Суммаризация с задержкой
                        var textToSummarize = string.IsNullOrWhiteSpace(article.Description)
                            ? article.Title
                            : article.Description;

                        article.Summary = await _chatService.SummarizeAsync(textToSummarize);
                        summarizedCount++;

                        // Задержка между AI запросами (чтобы не перегружать API)
                        await Task.Delay(DelayBetweenAiCallsMs);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex, "Failed to summarize article: {Title}. Will save without summary.",
                            article.Title?.Length > 50 ? article.Title[..50] + "..." : article.Title);

                        article.Summary = "⚠️ Суммаризация временно недоступна";
                    }

                // 4. Сохраняем все статьи (даже те, что без суммаризации)
                await _articleService.SaveArticlesAsync(newArticles);
                _logger.LogInformation(
                    "Saved {Count} articles from {SourceName} (summarized: {Summarized}, failed: {Failed})",
                    newArticles.Count, source, summarizedCount, failedCount);

                // 5. Задержка только для NewsAPI
                if (source.Url.Contains("newsapi.org"))
                    await Task.Delay(DelayForNewsApiMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source {SourceName}", source);
            }
    }
}