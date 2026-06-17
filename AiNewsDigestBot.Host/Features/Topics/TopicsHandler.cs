using AiNewsDigestBot.Host.Shared.Data.Enums;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.Topics;

public class TopicsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;
    private readonly SubscriptionService _subscriptionService;

    public TopicsHandler(ITelegramBotClient bot, UserService userService, SubscriptionService subscriptionService)
    {
        _bot = bot;
        _userService = userService;
        _subscriptionService = subscriptionService;
    }

    public async Task HandleAsync(long chatId)
    {
        // Находим пользователя
        var user = await _userService.GetUserAsync(chatId);
        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        // Получаем подписки пользователя
        var userSubscriptions = await _subscriptionService.GetUserSubscriptionTopicsAsync(user.Id);

        // Создаём inline клавиатуру
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        // Группа 1: Технологии
        var techRow = new List<InlineKeyboardButton>();
        techRow.Add(GetTopicButton(Topic.Technology, userSubscriptions));
        techRow.Add(GetTopicButton(Topic.ArtificialIntelligence, userSubscriptions));
        techRow.Add(GetTopicButton(Topic.Programming, userSubscriptions));
        inlineKeyboard.Add(techRow);

        // Группа 2: Безопасность и Наука
        var securityRow = new List<InlineKeyboardButton>();
        securityRow.Add(GetTopicButton(Topic.Security, userSubscriptions));
        securityRow.Add(GetTopicButton(Topic.Science, userSubscriptions));
        inlineKeyboard.Add(securityRow);

        // Группа 3: Бизнес и Здоровье
        var businessRow = new List<InlineKeyboardButton>();
        businessRow.Add(GetTopicButton(Topic.Business, userSubscriptions));
        businessRow.Add(GetTopicButton(Topic.Health, userSubscriptions));
        inlineKeyboard.Add(businessRow);

        // Группа 4: Спорт и Развлечения
        var sportsRow = new List<InlineKeyboardButton>();
        sportsRow.Add(GetTopicButton(Topic.Sports, userSubscriptions));
        sportsRow.Add(GetTopicButton(Topic.Entertainment, userSubscriptions));
        inlineKeyboard.Add(sportsRow);

        // Группа 5: Игры и Политика
        var gamingRow = new List<InlineKeyboardButton>();
        gamingRow.Add(GetTopicButton(Topic.Gaming, userSubscriptions));
        gamingRow.Add(GetTopicButton(Topic.Politics, userSubscriptions));
        inlineKeyboard.Add(gamingRow);

        // Кнопка "Мои подписки"
        var mySubsRow = new List<InlineKeyboardButton>();
        mySubsRow.Add(InlineKeyboardButton.WithCallbackData("📋 Мои подписки", "mysubs"));
        inlineKeyboard.Add(mySubsRow);

        var keyboard = new InlineKeyboardMarkup(inlineKeyboard);

        await _bot.SendMessage(
            chatId,
            "📋 *Выберите тему для подписки или отписки:*\n\n" +
            "✅ — вы подписаны\n" +
            "❌ — вы не подписаны\n\n" +
            "Нажмите на кнопку, чтобы изменить статус",
            ParseMode.Markdown,
            replyMarkup: keyboard);
    }

    private InlineKeyboardButton GetTopicButton(Topic topic, List<Topic> userSubscriptions)
    {
        var isSubscribed = userSubscriptions.Contains(topic);
        var emoji = isSubscribed ? "✅" : "❌";
        var buttonText = $"{emoji} {topic}";

        return InlineKeyboardButton.WithCallbackData(buttonText, $"topic_{topic}");
    }
}