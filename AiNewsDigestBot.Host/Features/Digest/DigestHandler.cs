using System.Net;
using System.Text.RegularExpressions;
using AiNewsDigestBot.Host.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace AiNewsDigestBot.Host.Features.Digest;

public class DigestHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public DigestHandler(AppDbContext db, ITelegramBotClient bot)
    {
        _db = db;
        _bot = bot;
    }

    public async Task HandleAsync(long chatId)
    {
        var user = await _db.Users
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.TelegramId == chatId);
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

        var articles = await _db.Articles
            .Where(a => subscribedTopics.Contains(a.Category))
            .OrderByDescending(a => a.PublishedAt)
            .Take(10)
            .ToListAsync();

        if (!articles.Any())
        {
            await _bot.SendMessage(chatId, "📭 Нет новостей по вашим подпискам");
            return;
        }

        var header = $"📰 Ваш дайджест ({articles.Count} статей)\n\n";
        List<string> messages = new();
        var currentMessage = header;

        foreach (var article in articles)
        {
            var title = StripHtml(article.Title);
            if (title.Length > 200) title = title[..200] + "...";

            var summary = StripHtml(article.Summary ?? article.Description ?? "Нет описания");
            if (summary.Length > 800) summary = summary[..800] + "...";

            var category = StripHtml(article.Category ?? "Без категории");
            var url = article.Url;

            var articleText = $"{title}\n📁 {category}\n{summary}\n🔗 {url}\n\n";

            if ((currentMessage + articleText).Length > 4000)
            {
                messages.Add(currentMessage);
                currentMessage = "📰 Дайджест (продолжение)\n\n";
            }

            currentMessage += articleText;
        }

        messages.Add(currentMessage);

        foreach (var msg in messages)
            // Отправляем как обычный текст (без parseMode)
            await _bot.SendMessage(chatId, msg);
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // Удаляем HTML-теги
        var noTags = Regex.Replace(input, "<.*?>", string.Empty);
        // Декодируем HTML-сущности (&nbsp; &lt; и т.д.)
        noTags = WebUtility.HtmlDecode(noTags);
        // Заменяем множественные пробелы и переносы строк на один пробел
        noTags = Regex.Replace(noTags, @"\s+", " ");
        return noTags.Trim();
    }
}