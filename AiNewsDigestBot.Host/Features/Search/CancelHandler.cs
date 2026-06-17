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

    public CancelHandler(ITelegramBotClient bot, SearchStateService searchState)
    {
        _bot = bot;
        _searchState = searchState;
    }

    public async Task HandleAsync(long chatId, StartHandler startHandler)
    {
        await _searchState.SetWaitingForSearchAsync(chatId, false);

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