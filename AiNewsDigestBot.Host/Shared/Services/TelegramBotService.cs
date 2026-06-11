using AiNewsDigestBot.Host.Features.Digest;
using AiNewsDigestBot.Host.Features.Help;
using AiNewsDigestBot.Host.Features.MySubs;
using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Features.Subscribe;
using AiNewsDigestBot.Host.Features.Topics;
using AiNewsDigestBot.Host.Features.Unsubscribe;
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
            // Определяем действие на основе команды или текста кнопки
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
                    // Проверяем текст кнопки
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
                        case "🔍 Последние новости":
                        case "/latest":
                            action = "latest";
                            break;
                        case "❓ Помощь":
                        case "/help":
                            action = "help";
                            break;
                        default:
                            action = "unknown";
                            break;
                    }

                    break;
            }

            // Обрабатываем действие
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

                case "settings":
                {
                    // TODO: добавить SettingsHandler позже
                    await bot.SendMessage(chatId, "⚙️ Настройка времени рассылки появится позже!");
                    break;
                }

                case "latest":
                {
                    // TODO: добавить LatestHandler позже
                    await bot.SendMessage(chatId, "📰 Последние новости появятся после добавления парсера!");
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

                var toggleHandler = scope.ServiceProvider.GetRequiredService<SubscribeHandler>();
                await toggleHandler.HandleAsync(chatId, topicName);

                var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                await topicsHandler.HandleAsync(chatId);

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