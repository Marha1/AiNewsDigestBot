using AiNewsDigestBot.Host.Shared.Data.Enums;
using Microsoft.AspNetCore.WebUtilities;

namespace AiNewsDigestBot.Host.Shared.Services.Parser;

public static class SourceHelper
{
    // Метки рекламы и аналитики: одной статье они дают разные адреса,
    // а на содержимое страницы не влияют. utm_* проверяется отдельно, по началу имени.
    private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "fbclid", "gclid", "yclid"
    };

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        
        url = url.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;
        var baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        
        var queryParams = QueryHelpers.ParseQuery(uri.Query)
            .Where(p => !IsTrackingParam(p.Key))
            .ToList();

        return queryParams.Count == 0
            ? baseUrl
            : QueryHelpers.AddQueryString(baseUrl, queryParams);
    }

    private static bool IsTrackingParam(string name) =>
        name.StartsWith("utm_", StringComparison.OrdinalIgnoreCase)
        || TrackingParams.Contains(name);

    public static SourceType DetectSource(string url)
    {
        if (url.Contains("newsapi.org") || url.Contains("newsapi"))
            return SourceType.NewsApi;
        if (url.Contains(".rss") || url.Contains("/rss") || url.Contains("/feed"))
            return SourceType.Rss;
        return SourceType.Html;
    }
}
