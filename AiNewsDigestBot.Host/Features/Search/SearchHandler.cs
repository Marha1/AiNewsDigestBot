using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Search;

public class SearchHandler
{
    private readonly ArticleService _articleService;
    private readonly ITelegramBotClient _bot;

    public SearchHandler(ArticleService articleService, ITelegramBotClient bot)
    {
        _articleService = articleService;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId, string query)
    {
        var articles = await _articleService.SearchArticlesAsync(query);

        if (!articles.Any())
        {
            await _bot.SendMessage(chatId, $"🔍 По запросу \"{query}\" ничего не найдено.");
            return;
        }

        var message = $"🔍 Результаты поиска: \"{query}\"\n\n";

        for (var i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            var summary = string.IsNullOrEmpty(article.Summary)
                ? article.Description ?? "Нет описания"
                : article.Summary;

            if (summary.Length > 200)
                summary = summary.Substring(0, 200) + "...";

            message += $"{i + 1}. {article.Title}\n";
            message += $"   Категория: {article.Category ?? "Без категории"}\n";
            message += $"   {summary}\n";
            message += $"   Ссылка: {article.Url}\n\n";

            if (message.Length > 3500 && i < articles.Count - 1)
            {
                await _bot.SendMessage(chatId, message);
                message = $"🔍 Результаты поиска: \"{query}\" (продолжение)\n\n";
            }
        }

        await _bot.SendMessage(chatId, message);
    }
}