using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enums;
using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public class NewsParser
{
    private readonly HttpClient _client;
    private readonly ILogger<NewsParser> _logger;
    private readonly string? _newApikey;

    public NewsParser(HttpClient client, IConfiguration config, ILogger<NewsParser> logger)
    {
        _client = client;
        _newApikey = config["NewsApi:Key"];
        _logger = logger;
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
            _logger.LogError(ex, "Parse error for {Url}: {Message}", url, ex.Message);
            return new List<Article>();
        }
    }

    private async Task<List<Article>> ParseNewsApiAsync(string url)
    {
        var articles = new List<Article>();

        try
        {
            if (string.IsNullOrEmpty(_newApikey))
            {
                _logger.LogWarning("NewsAPI key is not configured");
                return articles;
            }

            var apiKeyParam = url.Contains("?") ? $"&apiKey={_newApikey}" : $"?apiKey={_newApikey}";
            var fullUrl = url + apiKeyParam;

            _logger.LogDebug("Fetching NewsAPI: {Url}", fullUrl);

            var response = await _client.GetStringAsync(fullUrl);
            var result = JsonConvert.DeserializeObject<NewsApiResponse>(response);

            if (result?.Articles != null)
            {
                foreach (var item in result.Articles)
                {
                    var article = CreateArticle(
                        item.Title ?? "",
                        item.Url ?? "",
                        item.Description ?? "",
                        item.PublishedAt?.ToString() ?? "",
                        null
                    );

                    if (article != null)
                        articles.Add(article);
                }

                _logger.LogDebug("Parsed {Count} articles from NewsAPI", articles.Count);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching NewsAPI from {Url}: {Message}", url, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for NewsAPI response from {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while parsing NewsAPI from {Url}: {Message}", url, ex.Message);
        }

        return articles;
    }


    private async Task<List<Article>> ParseRssAsync(string url)
    {
        var articles = new List<Article>();

        try
        {
            _logger.LogDebug("Fetching RSS/Atom: {Url}", url);

            var content = await _client.GetStringAsync(url);

            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null
            });

            var feed = SyndicationFeed.Load(xmlReader);

            if (feed?.Items == null)
            {
                _logger.LogWarning("No items found in feed {Url}", url);
                return articles;
            }

            foreach (var item in feed.Items)
            {
                var title = item.Title?.Text ?? "";
                var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";

                var description = item.Summary?.Text ?? "";
                if (string.IsNullOrWhiteSpace(description))
                {
                    var contentItem = item.Content as TextSyndicationContent;
                    description = contentItem?.Text ?? "";
                }

                var cleanDescription = StripHtml(description);

                // ==================================================
                // ИСПРАВЛЕНИЕ: проверяем дату и передаём строку
                // ==================================================
                DateTime publishDate;
                if (item.PublishDate == DateTimeOffset.MinValue)
                {
                    // Нет даты — ставим текущее время
                    publishDate = DateTime.UtcNow;
                    _logger.LogDebug("Article '{Title}' has no publish date, using current time", title);
                }
                else
                {
                    publishDate = item.PublishDate.UtcDateTime;
                }

                var pubDateString = publishDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

                var article = CreateArticle(
                    title,
                    link,
                    cleanDescription,
                    pubDateString,
                    null
                );

                if (article != null)
                    articles.Add(article);
            }

            _logger.LogDebug("Parsed {Count} articles from feed {Url}", articles.Count, url);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching feed from {Url}: {Message}", url, ex.Message);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML parsing error for feed from {Url}: {Message}", url, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while parsing feed from {Url}: {Message}", url, ex.Message);
        }

        return articles;
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        try
        {
            var result = Regex.Replace(html, "<.*?>", string.Empty);
            result = Regex.Replace(result, @"\s+", " ");
            result = WebUtility.HtmlDecode(result);
            return result.Trim();
        }
        catch
        {
            return html;
        }
    }

    private Article? CreateArticle(string title, string link, string description, string pubDate,
        string? defaultCategory)
    {
        var normalizedUrl = NormalizeUrl(link);

        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            _logger.LogWarning("Skipping article with empty URL: {Title}", title);
            return null;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Skipping article with empty title: {Url}", normalizedUrl);
            return null;
        }

        var parsedDate = ParseDate(pubDate);

        return new Article
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = normalizedUrl,
            Description = description ?? "",
            Category = defaultCategory,
            PublishedAt = parsedDate,
            ParsedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Парсит дату. Если даты нет или это 01.01.0001 — ставит текущее время.
    /// </summary>
    private DateTime ParseDate(string dateString)
    {
        // Если строка пустая — сразу текущее время
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateString, out var date))
        {
            if (date.Year > 1)
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        return DateTime.UtcNow;
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        url = url.TrimEnd('/');

        var questionMarkIndex = url.IndexOf('?');
        if (questionMarkIndex > 0)
            url = url[..questionMarkIndex];

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