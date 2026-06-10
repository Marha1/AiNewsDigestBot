using AiNewsDigestBot.Host.Features.MySubs;
using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Features.Subscribe;
using AiNewsDigestBot.Host.Features.Topics;
using AiNewsDigestBot.Host.Features.Unsubscribe;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiNewsDigestBot.Host.Services.TelegramBot;

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
            if (command == "/start")
            {
                var startHandler = scope.ServiceProvider.GetRequiredService<StartHandler>();
                await startHandler.HandleAsync(chatId, message.Chat.Username, message.Chat.FirstName,
                    message.Chat.LastName);
            }
            else if (command == "/topics")
            {
                var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                await topicsHandler.HandleAsync(chatId);
            }
            else if (command == "/subscribe")
            {
                var topicName = parts.Length > 1 ? parts[1] : "";

                if (string.IsNullOrEmpty(topicName))
                {
                    await bot.SendMessage(chatId, "❌ Укажите тему. Пример: /subscribe Technology");
                    return;
                }

                var subscribeHandler = scope.ServiceProvider.GetRequiredService<SubscribeHandler>();
                await subscribeHandler.HandleAsync(chatId, topicName);
            }

            else if (command == "/unsubscribe")
            {
                var topicName = parts.Length > 1 ? parts[1] : "";

                if (string.IsNullOrEmpty(topicName))
                {
                    await bot.SendMessage(chatId, "❌ Укажите тему. Пример: /unsubscribe Technology");
                    return;
                }

                var unsubscribeHandler = scope.ServiceProvider.GetRequiredService<UnsubscribeHandler>();
                await unsubscribeHandler.HandleAsync(chatId, topicName);
            }
            else if (command == "/mysubs")
            {
                var mySubsHandler = scope.ServiceProvider.GetRequiredService<MySubsHandler>();
                await mySubsHandler.HandleAsync(chatId);
            }
            else
            {
                await bot.SendMessage(chatId, "❓ Неизвестная команда. Используйте /start");
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

            // 🔥 Новая обработка: возврат к списку всех тем
            if (data == "back_to_topics")
            {
                var topicsHandler = scope.ServiceProvider.GetRequiredService<TopicsHandler>();
                await topicsHandler.HandleAsync(chatId);
                await bot.AnswerCallbackQuery(callbackQuery.Id);
                return;
            }

            // 🔥 Новая обработка: отписка от темы из /mysubs
            if (data != null && data.StartsWith("unsubscribe_"))
            {
                var topicName = data.Replace("unsubscribe_", "");

                var unsubscribeHandler = scope.ServiceProvider.GetRequiredService<UnsubscribeHandler>();
                await unsubscribeHandler.HandleAsync(chatId, topicName);

                // Показываем обновлённый список подписок
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