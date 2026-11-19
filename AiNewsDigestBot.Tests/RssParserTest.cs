using AiNewsDigestBot.Host.Shared.Services.Parser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AiNewsDigestBot.Tests;

public class RssParserTest
{
    [Fact]
    public void TestRccParse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var config = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<NewsParser>>().Object;
        var parser = new NewsParser(httpClient, config.Object, logger);
        //Act
        var result =  parser.ParseAsync("https://feeds.bbci.co.uk/news/rss.xml").Result;
        //Assert 
        Assert.NotEmpty(result);
        foreach (var art in result)
        {

             Assert.Equal(DateTimeKind.Utc,art.PublishedAt.Kind);
             Assert.True(art.PublishedAt <=DateTime.UtcNow);
        }
        
    }
}