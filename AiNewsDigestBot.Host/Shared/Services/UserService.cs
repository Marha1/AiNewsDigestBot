// Shared/Services/UserService.cs

using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    ///     Получить пользователя по Telegram ID с настройками и подписками
    /// </summary>
    public async Task<User?> GetUserWithSettingsAndSubscriptionsAsync(long telegramId)
    {
        return await _db.Users
            .Include(u => u.Subscriptions)
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <summary>
    ///     Получить пользователя с настройками
    /// </summary>
    public async Task<User?> GetUserWithSettingsAsync(long telegramId)
    {
        return await _db.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <summary>
    ///     Получить пользователя по Telegram ID (без связанных данных)
    /// </summary>
    public async Task<User?> GetUserAsync(long telegramId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <summary>
    ///     Получить пользователя с подписками
    /// </summary>
    public async Task<User?> GetUserWithSubscriptionsAsync(long telegramId)
    {
        return await _db.Users
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <summary>
    ///     Получить всех пользователей с включённой рассылкой
    /// </summary>
    public async Task<List<User>> GetAllActiveUsersWithSettingsAsync()
    {
        return await _db.Users
            .Include(u => u.Settings)
            .Include(u => u.Subscriptions)
            .Where(u => u.Settings != null && u.Settings.IsEnabled)
            .ToListAsync();
    }

    /// <summary>
    ///     Получить пользователей для рассылки по времени
    /// </summary>
    public async Task<List<User>> GetUsersForDigestAsync(int hour, int minute)
    {
        return await _db.Users
            .Include(u => u.Settings)
            .Include(u => u.Subscriptions)
            .Where(u => u.Settings != null &&
                        u.Settings.IsEnabled &&
                        u.Settings.DigestHour == hour &&
                        u.Settings.DigestMinute == minute)
            .ToListAsync();
    }

    /// <summary>
    ///     Получить пользователя по ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
    }

    /// <summary>
    ///     Проверить, существует ли пользователь
    /// </summary>
    public async Task<bool> UserExistsAsync(long telegramId)
    {
        return await _db.Users.AnyAsync(u => u.TelegramId == telegramId);
    }

    /// <summary>
    ///     Добавить пользователя с настройками по умолчанию
    /// </summary>
    public async Task<User> AddUserAsync(long telegramId, string? username, string? firstName, string? lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);

        // Настройки по умолчанию
        _db.UserSettings.Add(new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DigestHour = 9,
            DigestMinute = 0,
            ArticlesPerDigest = 10,
            IsEnabled = true
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Added new user {TelegramId}", telegramId);

        return user;
    }

    /// <summary>
    ///     Обновить пользователя
    /// </summary>
    public async Task UpdateUserAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated user {TelegramId}", user.TelegramId);
    }

    /// <summary>
    ///     Обновить время последней активности
    /// </summary>
    public async Task UpdateLastActiveAsync(long telegramId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        if (user != null)
        {
            user.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    ///     Получить темы подписок пользователя
    /// </summary>
    public async Task<List<Topic>> GetUserTopicsAsync(long telegramId)
    {
        var user = await _db.Users
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);

        return user?.Subscriptions.Select(s => s.Topic).ToList() ?? new List<Topic>();
    }
}