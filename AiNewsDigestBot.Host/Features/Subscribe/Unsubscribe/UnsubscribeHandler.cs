using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Enum;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Unsubscribe;

public class UnsubscribeHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public UnsubscribeHandler(AppDbContext db, ITelegramBotClient bot)
    {
        _db = db;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId, string topicName)
    {
        // 1. Находим пользователя
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == chatId);
        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        // 2. Парсим тему
        if (!Enum.TryParse<Topic>(topicName, true, out var topic))
        {
            await _bot.SendMessage(chatId, $"❌ Неизвестная тема: {topicName}");
            return;
        }

        // 3. Находим подписку
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Topic == topic);

        if (subscription == null)
        {
            await _bot.SendMessage(chatId, $"❌ Вы не подписаны на тему {topic}");
            return;
        }

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();

        user.LastActiveAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _bot.SendMessage(chatId, $"✅ Вы отписались от темы {topic}");
    }
}