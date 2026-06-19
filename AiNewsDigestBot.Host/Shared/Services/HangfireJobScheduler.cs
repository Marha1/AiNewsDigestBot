using AiNewsDigestBot.Host.Shared.Services.Parser;
using Hangfire;

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
            _recurringJobManager.AddOrUpdate(
                "news-parsing",
                () => ParseNewsAsync(),
                "*/30 * * * *",
                TimeZoneInfo.Local);
            _recurringJobManager.AddOrUpdate(
                "digest-scheduler",
                () => CheckDigestsAsync(),
                "* * * * *",
                TimeZoneInfo.Local);

            _logger.LogInformation("Hangfire scheduler started: news parsing every 30 minutes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Hangfire scheduler");
        }
    }

    public void EnqueueInitialParse()
    {
        BackgroundJob.Enqueue<MainNewsParser>(p => p.ParseAndSaveAsync());
        _logger.LogInformation("Initial parse enqueued");
    }

    public async Task CheckDigestsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<DigestSchedulerService>();
        await scheduler.CheckAndSendDigestsAsync();
    }


    public async Task ParseNewsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var parser = scope.ServiceProvider.GetRequiredService<MainNewsParser>();
        await parser.ParseAndSaveAsync();
    }
}