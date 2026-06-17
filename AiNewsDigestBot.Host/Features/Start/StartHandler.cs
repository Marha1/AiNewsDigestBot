using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.Start;

public class StartHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;

    public StartHandler(ITelegramBotClient bot, UserService userService)
    {
        _bot = bot;
        _userService = userService;
    }

    public async Task HandleAsync(long chatId, string? username, string? firstName, string? lastName)
    {
        var isNewUser = false;
        var user = await _userService.GetUserAsync(chatId);

        if (user == null)
        {
            await _userService.AddUserAsync(chatId, username, firstName, lastName);
            isNewUser = true;
        }
        else
        {
            await _userService.UpdateLastActiveAsync(chatId);
        }
        var keyboard = GetMainKeyboard();
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

    public ReplyKeyboardMarkup GetMainKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📋 Темы"), new KeyboardButton("📰 Мои подписки") },
            new[] { new KeyboardButton("📊 Дайджест"), new KeyboardButton("⚙️ Настройки") },
            new[] { new KeyboardButton("📰 Последние новости"), new KeyboardButton("❓ Помощь") },
            new[] { new KeyboardButton("🔍 Поиск") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }
}