using AiNewsDigestBot.Host.Features.Search;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiNewsDigestBot.Host.Features.Settings;

public class SettingsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;
    private readonly SettingsStateService _settingsState;

    public SettingsHandler(ITelegramBotClient bot, UserService userService, SettingsStateService settingsState)
    {
        _bot = bot;
        _userService = userService;
        _settingsState = settingsState;
    }

    public async Task HandleAsync(long chatId, CancelHandler cancelHandler)
    {
        var user = await _userService.GetUserWithSettingsAsync(chatId);

        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        var settings = user.Settings;
        if (settings == null)
        {
            user.Settings = new UserSettings
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DigestHour = 9,
                DigestMinute = 0,
                ArticlesPerDigest = 10,
                IsEnabled = true
            };
            await _userService.UpdateUserAsync(user);
        }

        await SendKeyboardAsync(chatId, user.Settings);
    }

    private async Task SendKeyboardAsync(long chatId, UserSettings settings)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⏰ Время рассылки", "settings_time"),
                InlineKeyboardButton.WithCallbackData("📊 Количество статей", "settings_count")
            },
            new[]
            {
                settings.IsEnabled
                    ? InlineKeyboardButton.WithCallbackData("🔕 Отключить рассылку", "settings_disable")
                    : InlineKeyboardButton.WithCallbackData("🔔 Включить рассылку", "settings_enable")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад в меню", "back_to_menu")
            }
        });

        var status = settings.IsEnabled ? "✅ Включена" : "❌ Отключена";
        var message = $"⚙️ *Настройки*\n\n" +
                      $"🕐 Время рассылки: {settings.DigestHour:D2}:{settings.DigestMinute:D2}\n" +
                      $"📰 Количество статей: {settings.ArticlesPerDigest}\n" +
                      $"🔔 Статус: {status}\n\n" +
                      $"👇 Нажмите на кнопку для изменения";

        await _bot.SendMessage(chatId, message, ParseMode.Markdown, replyMarkup: keyboard);
    }

    public async Task HandleCallbackAsync(long chatId, string data, CancelHandler cancelHandler)
    {
        switch (data)
        {
            case "settings_time":
                await _settingsState.SetStateAsync(chatId, "settings_time");
                var timeKeyboard = cancelHandler.GetCancelKeyboard();
                await _bot.SendMessage(chatId,
                    "⏰ Введите время рассылки в формате HH:MM (например: 09:00)\n\nДля отмены нажмите кнопку ниже",
                    replyMarkup: timeKeyboard);
                break;

            case "settings_count":
                await _settingsState.SetStateAsync(chatId, "settings_count");
                var countKeyboard = cancelHandler.GetCancelKeyboard();
                await _bot.SendMessage(chatId,
                    "📊 Введите количество статей в дайджесте (от 1 до 50)\n\nДля отмены нажмите кнопку ниже",
                    replyMarkup: countKeyboard);
                break;

            case "settings_enable":
            case "settings_disable":
                var user = await _userService.GetUserWithSettingsAsync(chatId);
                if (user?.Settings != null)
                {
                    user.Settings.IsEnabled = data == "settings_enable";
                    await _userService.UpdateUserAsync(user);
                    
                    var status = user.Settings.IsEnabled ? "включена" : "отключена";
                    await _bot.SendMessage(chatId, $"✅ Рассылка {status}");
                    await SendKeyboardAsync(chatId, user.Settings);
                }
                break;
        }
    }

    public async Task HandleTimeInputAsync(long chatId, string input)
    {
        await _settingsState.SetStateAsync(chatId, null);

        var parts = input.Split(':');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var hour) ||
            !int.TryParse(parts[1], out var minute) ||
            hour < 0 || hour > 23 || minute < 0 || minute > 59)
        {
            await _bot.SendMessage(chatId, "❌ Неверный формат. Используйте HH:MM (например: 09:00)");
            return;
        }

        var user = await _userService.GetUserWithSettingsAsync(chatId);

        if (user?.Settings != null)
        {
            user.Settings.DigestHour = hour;
            user.Settings.DigestMinute = minute;
            await _userService.UpdateUserAsync(user);
            
            await _bot.SendMessage(chatId, $"✅ Время рассылки установлено: {hour:D2}:{minute:D2}");
            await SendKeyboardAsync(chatId, user.Settings);
        }
    }

    public async Task HandleCountInputAsync(long chatId, string input)
    {
        await _settingsState.SetStateAsync(chatId, null);

        if (!int.TryParse(input, out var count) || count < 1 || count > 50)
        {
            await _bot.SendMessage(chatId, "❌ Введите число от 1 до 50");
            return;
        }

        var user = await _userService.GetUserWithSettingsAsync(chatId);

        if (user?.Settings != null)
        {
            user.Settings.ArticlesPerDigest = count;
            await _userService.UpdateUserAsync(user);
            
            await _bot.SendMessage(chatId, $"✅ Количество статей установлено: {count}");
            await SendKeyboardAsync(chatId, user.Settings);
        }
    }
}