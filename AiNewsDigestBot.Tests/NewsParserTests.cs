using System.Net;
using System.Text;
using AiNewsDigestBot.Host.Shared.Services.Parser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace AiNewsDigestBot.Tests;

/// <summary>
/// Тесты для NewsParser: парсинг RSS/Atom лент
/// </summary>
public class NewsParserTests
{
    private HttpClient CreateMockHttpClient(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/xml")
            });
        return new HttpClient(handler.Object);
    }

    [Fact]
    public void ParseAsync_ParsesValidRssFeed()
    {
        // Arrange
        var rss = @"<?xml version=""1.0""?>
            <rss version=""2.0"">
                <channel>
                    <item>
                        <title>Article 1</title>
                        <link>https://example.com/1</link>
                        <description>Desc 1</description>
                        <pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate>
                    </item>
                    <item>
                        <title>Article 2</title>
                        <link>https://example.com/2</link>
                        <description>Desc 2</description>
                        <pubDate>Mon, 01 Jan 2024 13:00:00 GMT</pubDate>
                    </item>
                </channel>
            </rss>";

        var httpClient = CreateMockHttpClient(rss);
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);

        // Act
        var result = parser.ParseAsync("https://test.com/feed.xml", 25).Result;

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Article 1", result[0].Title);
        Assert.Equal("https://example.com/1", result[0].Url);
    }

    [Fact]
    public void ParseAsync_RespectsLimit()
    {
        // Arrange
        var rss = @"<?xml version=""1.0""?>
            <rss version=""2.0"">
                <channel>
                    <item><title>A1</title><link>https://example.com/1</link><description>D</description><pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate></item>
                    <item><title>A2</title><link>https://example.com/2</link><description>D</description><pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate></item>
                    <item><title>A3</title><link>https://example.com/3</link><description>D</description><pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate></item>
                </channel>
            </rss>";

        var httpClient = CreateMockHttpClient(rss);
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);

        // Act
        var result = parser.ParseAsync("https://test.com/feed.xml", 2).Result;

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseAsync_ReturnsEmpty_OnHttpError()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.NotFound);
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);

        // Act
        var result = parser.ParseAsync("https://test.com/feed.xml", 25).Result;

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_ReturnsEmpty_OnInvalidXml()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("not an xml");
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);

        // Act
        var result = parser.ParseAsync("https://test.com/feed.xml", 25).Result;

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_HandlesEmptyChannel()
    {
        // Arrange
        var rss = @"<?xml version=""1.0""?>
            <rss version=""2.0"">
                <channel>
                </channel>
            </rss>";

        var httpClient = CreateMockHttpClient(rss);
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);

        // Act
        var result = parser.ParseAsync("https://test.com/feed.xml", 25).Result;

        // Assert
        Assert.Empty(result);
    }
}