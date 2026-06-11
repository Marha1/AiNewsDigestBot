using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
        var isNewUser = false;

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
            isNewUser = true;
        }
        else
        {
            user.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Создаём клавиатуру с кнопками
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📋 Темы"), new KeyboardButton("📰 Мои подписки") },
            new[] { new KeyboardButton("📊 Дайджест"), new KeyboardButton("⚙️ Настройки") },
            new[] { new KeyboardButton("🔍 Последние новости"), new KeyboardButton("❓ Помощь") }
        })
        {
            ResizeKeyboard = true, // Подгоняем размер под экран
            OneTimeKeyboard = false // Клавиатура не скрывается после нажатия
        };

        string message;

        if (isNewUser)
            message = "✅ *Добро пожаловать в AI News Digest Bot!*\n\n" +
                      "Я буду присылать вам дайджест новостей по выбранным темам.\n\n" +
                      "📌 *Что я умею:*\n" +
                      "• Подписываться на темы новостей\n" +
                      "• Получать персональный дайджест\n" +
                      "• Настраивать время рассылки\n\n" +
                      "👇 *Используйте кнопки ниже для управления*";
        else
            message = "👋 *С возвращением!*\n\n" +
                      "👇 *Используйте кнопки ниже для управления*";

        await _bot.SendMessage(
            chatId,
            message,
            ParseMode.Markdown,
            replyMarkup: keyboard);
    }
}