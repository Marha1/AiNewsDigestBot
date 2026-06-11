using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public class MainNewsParser
{
    private const int DelayForNewsApiMs = 1000;
    private const int MaxArticlesPerSource = 15;
    
    private readonly IChatService _chatService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MainNewsParser> _logger;
    private readonly TopicDetector _topicDetector;
    private readonly ArticleService _articleService;
    private readonly SourceService _sourceService;


    public MainNewsParser(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<MainNewsParser> logger,
        TopicDetector topicDetector,
        IChatService chatService,
        ArticleService articleService,SourceService sourceService)  
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
        _topicDetector = topicDetector;
        _chatService = chatService;
        _articleService = articleService;
        _sourceService = sourceService;
    }

    public async Task ParseAndSaveAsync()
    {
        var httpClient = _httpFactory.CreateClient();
        var parser = new NewsParser(httpClient, _config);

        var sources = await _sourceService.GetActiveSourcesAsync();
        if (sources.Count == 0)
        {
            _logger.LogWarning("No active sources found.");
            return;
        }

        foreach (var source in sources)
        {
            try
            {
                
                // 1. Парсим с лимитом
                var articles = await parser.ParseAsync(source.Url, MaxArticlesPerSource);
                if (articles == null || articles.Count == 0)
                    continue;

                // 2. Дедупликация через ArticleService
                var newArticles = await _articleService.FilterNewArticlesAsync(articles);
                if (newArticles.Count == 0)
                {
                    continue;
                }

                // 3. Обрабатываем только новые статьи
                foreach (var article in newArticles)
                {
                    // Категория
                    var detectedCategory = _topicDetector.Detect(article.Title, article.Description);
                    article.Category = detectedCategory?.ToString()  ?? "General";
                    
                    // Связь с источником
                    article.SourceId = source.Id;
                    
                    // Суммаризация
                    var textToSummarize = string.IsNullOrWhiteSpace(article.Description)
                        ? article.Title
                        : article.Description;
                    article.Summary = await _chatService.SummarizeAsync(textToSummarize);
                }

                // 4. Сохраняем через ArticleService
                await _articleService.SaveArticlesAsync(newArticles);
                _logger.LogInformation("Saved and summarized {Count} new articles from {Name}", newArticles.Count);

                // 5. Задержка только для NewsAPI
                if (source.Url.Contains("newsapi.org"))
                    await Task.Delay(DelayForNewsApiMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source {Name}");
            }
        }
    }
}