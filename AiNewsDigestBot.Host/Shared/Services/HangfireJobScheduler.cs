using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Services.Parser;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class HangfireJobScheduler
{
    private readonly ILogger<HangfireJobScheduler> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IServiceProvider _serviceProvider;

    public HangfireJobScheduler(
        IRecurringJobManager recurringJobManager,
        IServiceProvider serviceProvider,
        ILogger<HangfireJobScheduler> logger)
    {
        _recurringJobManager = recurringJobManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void StartScheduler()
    {
        try
        {
            // Добавляем задачу парсинга новостей - запускается каждые 30 минут
            _recurringJobManager.AddOrUpdate(
                "news-parsing",
                () => ParseNewsAsync(),
                "*/30 * * * *", // Cron-выражение: каждые 30 минут
                TimeZoneInfo.Local);
            
            // Добавляем задачу проверки дайджестов - запускается каждую минуту
            _recurringJobManager.AddOrUpdate(
                "digest-scheduler",
                () => CheckDigestsAsync(),
                "* * * * *", // Cron-выражение: каждую минуту
                TimeZoneInfo.Local);

            _logger.LogInformation("Hangfire scheduler started: news parsing every 30 minutes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Hangfire scheduler");
        }
    }

    /// <summary>
    /// Запускает начальный парсинг только если в базе нет статей.
    /// Если статьи уже есть - пропускаем, чтобы не нагружать источники и OpenAI.
    /// </summary>
    public void EnqueueInitialParse()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Проверяем, есть ли хоть одна статья в базе
            var hasArticles = db.Articles.Any();
            
            if (!hasArticles)
            {
                // Статей нет - запускаем парсинг в фоне через Hangfire
                BackgroundJob.Enqueue<MainNewsParser>(p => p.ParseAndSaveAsync());
                _logger.LogInformation("Initial parse enqueued (no articles found)");
            }
            else
            {
                // Статьи уже есть - ничего не делаем, экономим ресурсы
                _logger.LogInformation("Skipping initial parse: articles already exist in database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check articles for initial parse");
        }
    }

    /// <summary>
    /// Проверяет, кому нужно отправить дайджест, и отправляет
    /// </summary>
    public async Task CheckDigestsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<DigestSchedulerService>();
        await scheduler.CheckAndSendDigestsAsync();
    }

    /// <summary>
    /// Запускает парсинг всех источников
    /// </summary>
    public async Task ParseNewsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var parser = scope.ServiceProvider.GetRequiredService<MainNewsParser>();
        await parser.ParseAndSaveAsync();
    }
}