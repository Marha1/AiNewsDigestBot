using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Latest;

public class LatestHandler
{
    private const int MaxMessageLength = 4000;
    private readonly ArticleService _articleService;
    private readonly ITelegramBotClient _bot;

    public LatestHandler(ArticleService articleService, ITelegramBotClient bot)
    {
        _articleService = articleService;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId)
    {
        var articles = await _articleService.GetLatestArticlesAsync(15);

        if (!articles.Any())
        {
            await _bot.SendMessage(chatId, "📭 Пока нет новостей. Попробуйте позже.");
            return;
        }

        var allMessages = new List<string>();
        var currentMessage = "📰 Последние новости\n\n";

        for (var i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            var summary = string.IsNullOrEmpty(article.Summary)
                ? article.Description ?? "Нет описания"
                : article.Summary;

            if (summary.Length > 200)
                summary = summary.Substring(0, 200) + "...";

            var articleText = $"{i + 1}. {article.Title}\n";
            articleText += $"   Категория: {article.Category ?? "Без категории"}\n";
            articleText += $"   {summary}\n";
            articleText += $"   Ссылка: {article.Url}\n\n";

            if (currentMessage.Length + articleText.Length > MaxMessageLength)
            {
                allMessages.Add(currentMessage);
                currentMessage = "📰 Последние новости (продолжение)\n\n";
            }

            currentMessage += articleText;
        }

        if (currentMessage.Length > 0)
            allMessages.Add(currentMessage);

        foreach (var message in allMessages) await _bot.SendMessage(chatId, message);
    }
}