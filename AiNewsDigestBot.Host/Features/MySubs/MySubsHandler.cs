using AiNewsDigestBot.Host.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.MySubs;

public class MySubsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public MySubsHandler(AppDbContext db, ITelegramBotClient bot)
    {
        _db = db;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId)
    {
        // Находим пользователя
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == chatId);
        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        var subscriptions = await _db.Subscriptions
            .Where(s => s.UserId == user.Id)
            .Select(s => s.Topic)
            .ToListAsync();

        if (!subscriptions.Any())
        {
            await _bot.SendMessage(chatId,
                "📭 У вас пока нет подписок.\n\n" +
                "Используйте /topics чтобы посмотреть доступные темы");
            return;
        }

        // Создаём клавиатуру с кнопками для отписки
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        foreach (var topic in subscriptions)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(InlineKeyboardButton.WithCallbackData($"❌ Отписаться от {topic}", $"unsubscribe_{topic}"));
            inlineKeyboard.Add(row);
        }

        var backRow = new List<InlineKeyboardButton>();
        backRow.Add(InlineKeyboardButton.WithCallbackData("📋 Все темы", "back_to_topics"));
        inlineKeyboard.Add(backRow);

        var keyboard = new InlineKeyboardMarkup(inlineKeyboard);

        var message = "📋 *Ваши подписки:*\n\n";
        foreach (var topic in subscriptions) message += $"✅ {topic}\n";

        message += "\n👇 Нажмите на кнопку, чтобы отписаться";

        await _bot.SendMessage(chatId, message,
            ParseMode.Markdown,
            replyMarkup: keyboard);
    }
}