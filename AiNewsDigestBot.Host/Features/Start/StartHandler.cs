using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Start;

public class StartHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public StartHandler(AppDbContext db, ITelegramBotClient bot)
    {
        _db = db;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId, string? username, string? firstName, string? lastName)
    {
        // Проверяем, есть ли пользователь
        var user = _db.Users.FirstOrDefault(u => u.TelegramId == chatId);

        if (user == null)
        {
            // Создаём нового
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = chatId,
                UserName = username,
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

            await _bot.SendMessage(chatId,
                "✅ Добро пожаловать!\n\n" +
                "📌 Команды:\n" +
                "/topics — список тем\n" +
                "/subscribe <тема> — подписаться\n" +
                "/digest — дайджест сейчас\n" +
                "/settings — настройки");
        }
        else
        {
            user.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _bot.SendMessage(chatId, "👋 С возвращением! Используйте /topics");
        }
    }
}