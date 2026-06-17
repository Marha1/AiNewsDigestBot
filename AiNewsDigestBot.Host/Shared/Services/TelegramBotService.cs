using AiNewsDigestBot.Host.Features.Digest;
using AiNewsDigestBot.Host.Features.Help;
using AiNewsDigestBot.Host.Features.Latest;
using AiNewsDigestBot.Host.Features.MySubs;
using AiNewsDigestBot.Host.Features.Search;
using AiNewsDigestBot.Host.Features.Settings;
using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Features.Subscribe;
using AiNewsDigestBot.Host.Features.Subscribe.Unsubscribe;
using AiNewsDigestBot.Host.Features.Topics;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiNewsDigestBot.Host.Shared.Services;

public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _services;

    public TelegramBotService(ITelegramBotClient bot, IServiceProvider services, ILogger<TelegramBotService> logger)
    {
        _bot = bot;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Бот запущен");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(bot, update.CallbackQuery, ct);
            return;
        }

        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        var chatId = message.Chat.Id;
        var parts = messageText.Split(' ');
        var command = parts[0].ToLower();

        using var scope = _services.CreateScope();

        try
        {
            var searchState = scope.ServiceProvider.GetRequiredService<SearchStateService>();
            var isWaitingForSearch = await searchState.IsWaitingForSearchAsync(chatId);

            if (isWaitingForSearch)
            {
                var searchHandler = scope.ServiceProvider.GetRequiredService<SearchHandler>();
                await searchHandler.HandleAsync(chatId, messageText);
                return;
            }

            var settingsState = scope.ServiceProvider.GetRequiredService<SettingsStateService>();
            var currentState = await settingsState.GetStateAsync(chatId);
            if (currentState == "settings_time")
            {
                var settingsHandler = scope.ServiceProvider.GetRequiredService<SettingsHandler>();
                await settingsHandler.HandleTimeInputAsync(chatId, messageText);
                return;
            }

            if (currentState == "settings_count")
            {
                var settingsHandler = scope.ServiceProvider.GetRequiredService<SettingsHandler>();
                await settingsHandler.HandleCountInputAsync(chatId, messageText);
                return;
            }

            string action;
            switch (command)
            {
                case "/start":
                    action = "start";
                    break;
                case "/subscribe":
                    action = "subscribe";
                    break;
                case "/unsubscribe":
                    action = "unsubscribe";
                    break;
                default:
                    switch (messageText)
                    {
                        case "📋 Темы":
                        case "/topics":
                            action = "topics";
                            break;
                        case "📰 Мои подписки":
                        case "/mysubs":
                            action = "mysubs";
                            break;
                        case "📊 Дайджест":
                        case "/digest":
                            action = "digest";
                            break;
                        case "⚙️ Настройки":
                        case "/settings":
                            action = "settings";
                            break;
                        case "🔍 Поиск":
                        case "/search":
                            action = "search";
                            break;
                        case "📰 Последние новости":
                        case "/latest":
                            action = "latest";
                            break;
                        case "❓ Помощь":
                        case "/help":
                            action = "help";
                            break;
                        case "/cancel":
                        case "❌ Отмена":
                            action = "cancel";
                            break;
                        default:
                            action = "unknown";
                            break;
                    }

                    break;
            }

            switch (action)
            {
                case "start":
                {
                    var startHandler = scope.ServiceProvider.GetRequiredService<StartHandler>();
                    await startHandler.HandleAsync(chatId, message.Chat.Username, message.Chat.FirstName,
                        message.Chat.LastName);
                    break;
                }

                case "topics":
                {
                    var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                    await topicsHandler.HandleAsync(chatId);
                    break;
                }

                case "subscribe":
                {
                    var topicName = parts.Length > 1 ? parts[1] : "";

                    if (string.IsNullOrEmpty(topicName))
                    {
                        await bot.SendMessage(chatId, "❌ Укажите тему. Пример: /subscribe Technology");
                        return;
                    }

                    var subscribeHandler = scope.ServiceProvider.GetRequiredService<SubscribeHandler>();
                    await subscribeHandler.HandleAsync(chatId, topicName);
                    break;
                }

                case "unsubscribe":
                {
                    var topicName = parts.Length > 1 ? parts[1] : "";

                    if (string.IsNullOrEmpty(topicName))
                    {
                        await bot.SendMessage(chatId, "❌ Укажите тему. Пример: /unsubscribe Technology");
                        return;
                    }

                    var unsubscribeHandler = scope.ServiceProvider.GetRequiredService<UnsubscribeHandler>();
                    await unsubscribeHandler.HandleAsync(chatId, topicName);
                    break;
                }

                case "mysubs":
                {
                    var mySubsHandler = scope.ServiceProvider.GetRequiredService<MySubsHandler>();
                    await mySubsHandler.HandleAsync(chatId);
                    break;
                }

                case "help":
                {
                    var helpHandler = scope.ServiceProvider.GetRequiredService<HelpHandler>();
                    await helpHandler.ShowHelpAsync(chatId);
                    break;
                }

                case "digest":
                {
                    var digestHandler = scope.ServiceProvider.GetRequiredService<DigestHandler>();
                    await digestHandler.HandleAsync(chatId);
                    break;
                }

                case "search":
                {
                    await searchState.SetWaitingForSearchAsync(chatId, true);

                    var cancelHandler = scope.ServiceProvider.GetRequiredService<CancelHandler>();
                    var cancelKeyboard = cancelHandler.GetCancelKeyboard();

                    await bot.SendMessage(chatId,
                        "🔍 Введите поисковый запрос (например: Apple или ИИ)\n\nДля отмены нажмите кнопку ниже",
                        replyMarkup: cancelKeyboard);
                    break;
                }
                case "cancel":
                {
                    var cancelHandler = scope.ServiceProvider.GetRequiredService<CancelHandler>();
                    var startHandler = scope.ServiceProvider.GetRequiredService<StartHandler>();
                    await cancelHandler.HandleAsync(chatId, startHandler);
                    break;
                }

                case "latest":
                {
                    var latestHandler = scope.ServiceProvider.GetRequiredService<LatestHandler>();
                    await latestHandler.HandleAsync(chatId);
                    break;
                }

                case "settings":
                {
                    var settingsHandler = scope.ServiceProvider.GetRequiredService<SettingsHandler>();
                    var сancelHandler = scope.ServiceProvider.GetRequiredService<CancelHandler>();
                    await settingsHandler.HandleAsync(chatId, сancelHandler);
                    break;
                }

                case "unknown":
                default:
                {
                    await bot.SendMessage(chatId, "❓ Неизвестная команда. Используйте /start или кнопки меню");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в команде {Command}", command);
            await bot.SendMessage(chatId, "❌ Ошибка. Попробуйте позже.");
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery,
        CancellationToken ct)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data;

        using var scope = _services.CreateScope();

        try
        {
            if (data == "mysubs")
            {
                var mySubsHandler = scope.ServiceProvider.GetRequiredService<MySubsHandler>();
                await mySubsHandler.HandleAsync(chatId);
                await bot.AnswerCallbackQuery(callbackQuery.Id);
                return;
            }

            if (data == "back_to_topics")
            {
                var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                await topicsHandler.HandleAsync(chatId);
                await bot.AnswerCallbackQuery(callbackQuery.Id);
                return;
            }

            if (data != null && data.StartsWith("unsubscribe_"))
            {
                var topicName = data.Replace("unsubscribe_", "");

                var unsubscribeHandler = scope.ServiceProvider.GetRequiredService<UnsubscribeHandler>();
                await unsubscribeHandler.HandleAsync(chatId, topicName);

                var mySubsHandler = scope.ServiceProvider.GetRequiredService<MySubsHandler>();
                await mySubsHandler.HandleAsync(chatId);

                await bot.AnswerCallbackQuery(callbackQuery.Id, $"✅ Вы отписались от {topicName}");
                return;
            }

            if (data != null && data.StartsWith("topic_"))
            {
                var topicName = data.Replace("topic_", "");

                var subscribeHandler = scope.ServiceProvider.GetRequiredService<SubscribeHandler>();
                await subscribeHandler.HandleAsync(chatId, topicName);

                var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                await topicsHandler.HandleAsync(chatId);

                await bot.AnswerCallbackQuery(callbackQuery.Id);
                return;
            }

            if (data != null && (data.StartsWith("settings_") || data == "back_to_menu"))
            {
                var settingsHandler = scope.ServiceProvider.GetRequiredService<SettingsHandler>();
                var cancelHandler = scope.ServiceProvider.GetRequiredService<CancelHandler>();
                await settingsHandler.HandleCallbackAsync(chatId, data, cancelHandler);
                await bot.AnswerCallbackQuery(callbackQuery.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в callback {Data}", data);
            await bot.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Ошибка бота");
        return Task.CompletedTask;
    }
}