using AiNewsDigestBot.Host.Shared.Data.Enums;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Subscribe.Unsubscribe;

public class UnsubscribeHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SubscriptionService _subscriptionService;
    private readonly UserService _userService;

    public UnsubscribeHandler(ITelegramBotClient bot, UserService userService, SubscriptionService subscriptionService)
    {
        _bot = bot;
        _userService = userService;
        _subscriptionService = subscriptionService;
    }

    public async Task HandleAsync(long chatId, string topicName)
    {
        // 1. Находим пользователя
        var user = await _userService.GetUserAsync(chatId);
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

        // 3. Удаляем подписку
        var removed = await _subscriptionService.RemoveSubscriptionAsync(user.Id, topic);

        if (!removed)
        {
            await _bot.SendMessage(chatId, $"❌ Вы не подписаны на тему {topic}");
            return;
        }

        // 4. Обновляем активность пользователя
        await _userService.UpdateLastActiveAsync(chatId);

        await _bot.SendMessage(chatId, $"✅ Вы отписались от темы {topic}");
    }
}