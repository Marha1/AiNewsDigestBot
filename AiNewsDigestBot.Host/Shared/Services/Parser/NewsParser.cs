using System.Xml;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enums;
using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public class NewsParser
{
    private readonly HttpClient _client;
    private readonly string? _newApikey;

    public NewsParser(HttpClient client, IConfiguration config)
    {
        _client = client;
        _newApikey = config["NewsApi:Key"];
    }

    public async Task<List<Article>> ParseAsync(string url, int limit = 25)
    {
        try
        {
            var sourceType = await DetectSource(url);
            var articles = sourceType switch
            {
                SourceType.Rss => await ParseRssAsync(url),
                SourceType.NewsApi => await ParseNewsApiAsync(url),
                _ => new List<Article>()
            };

            return articles.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parse error for {url}: {ex.Message}");
            return new List<Article>();
        }
    }

    /// <summary>
    ///     Парсинг NewsAPI
    /// </summary>
    private async Task<List<Article>> ParseNewsApiAsync(string url)
    {
        var articles = new List<Article>();

        try
        {
            var apiKeyParam = url.Contains("?") ? $"&apiKey={_newApikey}" : $"?apiKey={_newApikey}";
            var fullUrl = url + apiKeyParam;
            var response = await _client.GetStringAsync(fullUrl);
            var result = JsonConvert.DeserializeObject<NewsApiResponse>(response);

            if (result?.Articles != null)
                foreach (var item in result.Articles)
                {
                    var article = CreateArticle(
                        item.Title ?? "",
                        item.Url ?? "",
                        item.Description ?? "",
                        item.PublishedAt?.ToString() ?? "",
                        null,
                        null
                    );
                    articles.Add(article);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NewsAPI parse error for {url}: {ex.Message}");
        }

        return articles;
    }


    private async Task<List<Article>> ParseRssAsync(string url)
    {
        var articles = new List<Article>();
        var sourceName = "Unknown RSS";

        try
        {
            var content = await _client.GetStringAsync(url);
            var doc = new XmlDocument();
            doc.LoadXml(content);

            var channelNode = doc.SelectSingleNode("//channel/title");
            if (channelNode != null && !string.IsNullOrWhiteSpace(channelNode.InnerText))
            {
                sourceName = channelNode.InnerText.Trim();
                if (sourceName.Contains(":"))
                    sourceName = sourceName.Split(':')[0].Trim();
                if (sourceName.Contains("|"))
                    sourceName = sourceName.Split('|')[0].Trim();
            }

            var items = doc.SelectNodes("//item");
            if (items != null)
                foreach (XmlNode item in items)
                {
                    var article = CreateArticle(
                        item.SelectSingleNode("title")?.InnerText ?? "",
                        item.SelectSingleNode("link")?.InnerText ?? "",
                        item.SelectSingleNode("description")?.InnerText ?? "",
                        item.SelectSingleNode("pubDate")?.InnerText ?? "",
                        sourceName, // ← передаём название источника
                        null
                    );
                    articles.Add(article);
                }
        }
        catch (Exception e)
        {
            Console.WriteLine($"RSS parse error for {url}: {e.Message}");
        }

        return articles;
    }

    private Article CreateArticle(string title, string link, string description, string pubDate,
        string sourceName, string? defaultCategory)
    {
        var normalizedUrl = NormalizeUrl(link);
    
        return new Article
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = normalizedUrl,
            Description = description,
            Category = defaultCategory,
            PublishedAt = DateTime.TryParse(pubDate, out var date)
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : DateTime.UtcNow,
            ParsedAt = DateTime.UtcNow
        };
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
    
        // Убираем слеш в конце
        url = url.TrimEnd('/');
    
        // Приводим к нижнему регистру
        return url.ToLowerInvariant();
    }


    private async Task<SourceType> DetectSource(string url)
    {
        if (url.Contains("newsapi.org") || url.Contains("newsapi"))
            return SourceType.NewsApi;
        if (url.Contains(".rss") || url.Contains("/rss") || url.Contains("/feed"))
            return SourceType.Rss;
        return SourceType.Html;
    }

    private class NewsApiResponse
    {
        public List<NewsApiArticle> Articles { get; } = new();
    }
}