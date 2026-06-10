using System.Xml;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enums;

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

    public async Task<List<Article>> ParseAsync(string url, string sourceName, string? defaultCategory = null)
    {
        try
        {
            var sourceType = await DetectSource(url);
            return sourceType switch
            {
                SourceType.Rss => await ParseRssAsync(url, sourceName, defaultCategory)
            };
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error for {url}: {ex.Message}");
            return new List<Article>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    private async Task<List<Article>> ParseRssAsync(string url, string sourceName, string? defaultCategory)
    {
        var articles = new List<Article>();
        try
        {
            var content = await _client.GetStringAsync(url);
            var doc = new XmlDocument();
            doc.LoadXml(content);
            var items = doc.SelectNodes("//item");
            if (items != null)
                foreach (XmlNode item in items)
                {
                    var article = CreateArticle(
                        item.SelectSingleNode("title")?.InnerText ?? "",
                        item.SelectSingleNode("link")?.InnerText ?? "",
                        item.SelectSingleNode("description")?.InnerText ?? "",
                        item.SelectSingleNode("pubDate")?.InnerText ?? "",
                        sourceName,
                        defaultCategory
                    );
                    articles.Add(article);
                }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return articles;
    }

    /// <summary>
    ///     Создание объекта Article
    /// </summary>
    private Article CreateArticle(string title, string link, string description, string pubDate,
        string sourceName, string? defaultCategory)
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = link,
            Description = description,
            Source = sourceName,
            Category = defaultCategory,
            PublishedAt = ParseDate(pubDate),
            ParsedAt = DateTime.UtcNow,
            IsSummarized = false
        };
    }

    private DateTime ParseDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out var date))
            return date;
        return DateTime.UtcNow;
    }

    private async Task<SourceType> DetectSource(string url)
    {
        if (url.Contains("newsapi.org") || url.Contains("newsapi"))
            return SourceType.NewsApi;
        if (url.Contains(".rss") || url.Contains("/rss") || url.Contains("/feed"))
            return SourceType.Rss;
        return SourceType.Html;
    }
}