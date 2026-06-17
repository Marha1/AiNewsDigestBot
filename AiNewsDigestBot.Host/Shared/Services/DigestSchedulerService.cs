using AiNewsDigestBot.Host.Features.Digest;
using AiNewsDigestBot.Host.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class DigestSchedulerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DigestSchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DigestSchedulerService(AppDbContext db, IServiceProvider serviceProvider,
        ILogger<DigestSchedulerService> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    ///     Проверяет каждую минуту, кому нужно отправить дайджест
    /// </summary>
    public async Task CheckAndSendDigestsAsync()
    {
        var now = DateTime.Now;
        var currentMinute = now.Minute;
        var currentHour = now.Hour;

        _logger.LogDebug("Checking digests at {Hour}:{Minute}", currentHour, currentMinute);

        var users = await _db.Users
            .Include(u => u.Settings)
            .Include(u => u.Subscriptions)
            .Where(u => u.Settings != null &&
                        u.Settings.IsEnabled &&
                        u.Settings.DigestHour == currentHour &&
                        u.Settings.DigestMinute == currentMinute)
            .ToListAsync();

        if (!users.Any())
        {
            _logger.LogDebug("No users to send digests at {Hour}:{Minute}", currentHour, currentMinute);
            return;
        }

        _logger.LogInformation("Found {Count} users to send digests", users.Count);

        foreach (var user in users)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var digestHandler = scope.ServiceProvider.GetRequiredService<DigestHandler>();

                _logger.LogInformation("Sending digest to user {TelegramId} at {Time}", user.TelegramId, now);

                await digestHandler.HandleAsync(user.TelegramId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending digest to user {TelegramId}", user.TelegramId);
            }
    }
}