using System.Net;
using System.Text.RegularExpressions;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AiNewsDigestBot.Host.Features.Digest;

public class DigestHandler
{
    private readonly ArticleService _articleService;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<DigestHandler> _logger;
    private readonly UserService _userService;

    public DigestHandler(
        ITelegramBotClient bot,
        ILogger<DigestHandler> logger,
        UserService userService,
        ArticleService articleService)
    {
        _bot = bot;
        _logger = logger;
        _userService = userService;
        _articleService = articleService;
    }

    public async Task HandleAsync(long chatId)
    {
        var user = await _userService.GetUserWithSettingsAndSubscriptionsAsync(chatId);

        if (user == null)
        {
            await _bot.SendMessage(chatId, "❌ Сначала отправьте /start");
            return;
        }

        var subscribedTopics = user.Subscriptions.Select(s => s.Topic.ToString()).ToList();

        if (!subscribedTopics.Any())
        {
            await _bot.SendMessage(chatId, "📭 У вас нет подписок. Используйте /topics");
            return;
        }

        var limit = user.Settings?.ArticlesPerDigest ?? 10;

        var articles = await _articleService.GetArticlesByTopicsAsync(subscribedTopics, limit);

        if (!articles.Any())
        {
            await _bot.SendMessage(chatId, "📭 Нет новостей по вашим подпискам");
            return;
        }

        await SendDigestInternalAsync(chatId, articles);
    }

    private async Task SendDigestInternalAsync(long chatId, List<Article> articles)
    {
        var header = $"📰 *Ваш дайджест* ({articles.Count} статей)\n\n";
        var messages = new List<string>();
        var currentMessage = header;

        foreach (var article in articles)
        {
            var title = StripHtml(article.Title);
            if (title.Length > 200) title = title[..200] + "...";

            var summary = StripHtml(article.Summary ?? article.Description ?? "Нет описания");
            if (summary.Length > 800) summary = summary[..800] + "...";

            var category = StripHtml(article.Category ?? "Без категории");
            var url = article.Url;

            var articleText = $"*{title}*\n📁 {category}\n{summary}\n[Читать далее]({url})\n\n";

            if ((currentMessage + articleText).Length > 4000)
            {
                messages.Add(currentMessage);
                currentMessage = "📰 *Дайджест (продолжение)*\n\n";
            }

            currentMessage += articleText;
        }

        messages.Add(currentMessage);

        foreach (var msg in messages) await _bot.SendMessage(chatId, msg, ParseMode.Markdown);
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var noTags = Regex.Replace(input, "<.*?>", string.Empty);
        noTags = WebUtility.HtmlDecode(noTags);
        noTags = Regex.Replace(noTags, @"\s+", " ");
        return noTags.Trim();
    }
}