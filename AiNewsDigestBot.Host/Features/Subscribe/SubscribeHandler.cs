using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enum;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Subscribe;

public class SubscribeHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public SubscribeHandler(AppDbContext db, ITelegramBotClient bot)
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

        // 2. Парсим название темы
        if (!Enum.TryParse<Topic>(topicName, true, out var topic))
        {
            await _bot.SendMessage(chatId,
                $"❌ Неизвестная тема: {topicName}\n\n" +
                $"Доступные темы: {string.Join(", ", Enum.GetNames<Topic>())}");
            return;
        }

        // 3. Проверяем, не подписан ли уже
        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Topic == topic);

        if (existing != null)
        {
            await _bot.SendMessage(chatId, $"❌ Вы уже подписаны на тему {topic}");
            return;
        }

        // 4. Создаём подписку
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Topic = topic,
            SubscribedAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        // 5. Обновляем активность пользователя
        user.LastActiveAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 6. Отвечаем
        await _bot.SendMessage(chatId,
            $"✅ Вы подписались на тему {topic}\n\nИспользуйте /topics чтобы увидеть все подписки");
    }
}