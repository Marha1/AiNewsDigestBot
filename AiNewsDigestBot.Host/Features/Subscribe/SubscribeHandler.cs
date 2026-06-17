using AiNewsDigestBot.Host.Shared.Data.Enums;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Subscribe;

public class SubscribeHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;
    private readonly SubscriptionService _subscriptionService;

    public SubscribeHandler(ITelegramBotClient bot, UserService userService, SubscriptionService subscriptionService)
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

        // 2. Парсим название темы
        if (!Enum.TryParse<Topic>(topicName, true, out var topic))
        {
            await _bot.SendMessage(chatId,
                $"❌ Неизвестная тема: {topicName}\n\n" +
                $"Доступные темы: {string.Join(", ", Enum.GetNames<Topic>())}");
            return;
        }

        // 3. Проверяем, не подписан ли уже
        var isSubscribed = await _subscriptionService.IsUserSubscribedAsync(user.Id, topic);
        if (isSubscribed)
        {
            // Если подписан - отписываем
            await _subscriptionService.RemoveSubscriptionAsync(user.Id, topic);
            await _bot.SendMessage(chatId, $"❌ Вы отписались от темы {topic}");
            return;
        }

        // 4. Создаём подписку
        await _subscriptionService.AddSubscriptionAsync(user.Id, topic);

        // 5. Обновляем активность пользователя
        await _userService.UpdateLastActiveAsync(chatId);

        // 6. Отвечаем
        await _bot.SendMessage(chatId,
            $"✅ Вы подписались на тему {topic}\n\nИспользуйте /topics чтобы увидеть все подписки");
    }
}