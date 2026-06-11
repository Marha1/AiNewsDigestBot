using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AiNewsDigestBot.Host.Features.Help;

public class HelpHandler
{
    private readonly ITelegramBotClient _bot;

    public HelpHandler(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task ShowHelpAsync(long chatId)
    {
        var helpMessage =
            "🤖 *Помощь по боту*\n\n"
            + "📋 *Темы* - выбрать темы для подписки\n"
            + "📰 *Мои подписки* - просмотреть текущие подписки\n"
            + "📊 *Дайджест* - получить персональный дайджест сейчас\n"
            + "⚙️ *Настройки* - настроить время рассылки\n"
            + "🔍 *Последние новости* - свежие новости без фильтра\n\n"
            + "Также доступны текстовые команды:\n"
            + "/subscribe Technology - подписаться на тему\n"
            + "/unsubscribe Technology - отписаться от темы\n\n"
            + "📌 *Доступные темы:*\n"
            + "Technology, Science, Business, Health, Sports, Entertainment,\n"
            + "ArtificialIntelligence, Programming, Security, Politics, Gaming";

        await _bot.SendMessage(chatId, helpMessage, ParseMode.Markdown);
    }
}