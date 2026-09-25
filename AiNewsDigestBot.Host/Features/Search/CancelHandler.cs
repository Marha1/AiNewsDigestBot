// Features/Cancel/CancelHandler.cs

using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.Search;

public class CancelHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SearchStateService _searchState;
    private readonly SettingsStateService _settingsStateService;

    public CancelHandler(ITelegramBotClient bot, SearchStateService searchState, SettingsStateService settingsStateService)
    {
        _bot = bot;
        _searchState = searchState;
        _settingsStateService = settingsStateService;
    }

    public async Task HandleAsync(long chatId, StartHandler startHandler)
    {
        await _searchState.SetWaitingForSearchAsync(chatId, false);
        await _settingsStateService.SetStateAsync(chatId, string.Empty);
        var keyboard = startHandler.GetMainKeyboard();
        await _bot.SendMessage(chatId, "✅ Возвращаю обратно.", replyMarkup: keyboard);
    }

    public ReplyKeyboardMarkup GetCancelKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("❌ Отмена") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
}