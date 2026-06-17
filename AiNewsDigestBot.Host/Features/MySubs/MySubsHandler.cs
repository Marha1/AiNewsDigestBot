using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.MySubs;

public class MySubsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;
    private readonly SubscriptionService _subscriptionService;

    public MySubsHandler(ITelegramBotClient bot, UserService userService, SubscriptionService subscriptionService)
    {
        _bot = bot;
        _userService = userService;
        _subscriptionService = subscriptionService;
    }

    public async Task HandleAsync(long chatId)
    {
        var user = await _userService.GetUserAsync(chatId);
        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(user.Id);

        if (!subscriptions.Any())
        {
            await _bot.SendMessage(chatId,
                "📭 У вас пока нет подписок.\n\n" +
                "Используйте /topics чтобы посмотреть доступные темы");
            return;
        }

        // Создаём клавиатуру с кнопками для отписки
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        foreach (var subscription in subscriptions)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(InlineKeyboardButton.WithCallbackData($"❌ Отписаться от {subscription.Topic}", $"unsubscribe_{subscription.Topic}"));
            inlineKeyboard.Add(row);
        }

        var backRow = new List<InlineKeyboardButton>();
        backRow.Add(InlineKeyboardButton.WithCallbackData("📋 Все темы", "back_to_topics"));
        inlineKeyboard.Add(backRow);

        var keyboard = new InlineKeyboardMarkup(inlineKeyboard);

        var message = "📋 *Ваши подписки:*\n\n";
        foreach (var subscription in subscriptions) 
            message += $"✅ {subscription.Topic}\n";

        message += "\n👇 Нажмите на кнопку, чтобы отписаться";

        await _bot.SendMessage(chatId, message,
            ParseMode.Markdown,
            replyMarkup: keyboard);
    }
}