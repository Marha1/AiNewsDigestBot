using System.Net;
using System.Text;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace AiNewsDigestBot.Tests;

/// <summary>
/// Тесты для ChatService: суммаризация текста через OpenAI API
/// </summary>
public class ChatServiceTests
{
    [Fact]
    public void SummarizeAsync_ReturnsSummary_WhenApiReturnsValidResponse()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");

        var json = @"
        {
            ""choices"": [
                {
                    ""index"": 0,
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""This is a test summary""
                    },
                    ""finish_reason"": ""stop""
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = new Mock<ILogger<ChatService>>().Object;
        var service = new ChatService(httpClient, config.Object, logger);

        // Act
        var result = service.SummarizeAsync("Test article").Result;

        // Assert
        Assert.Equal("This is a test summary", result);
    }

    [Fact]
    public void SummarizeAsync_ReturnsFallback_WhenApiKeyMissing()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["OpenAI:ApiKey"]).Returns((string?)null);

        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<ChatService>>().Object;
        var service = new ChatService(httpClient, config.Object, logger);

        // Act
        var result = service.SummarizeAsync("Test article").Result;

        // Assert
        Assert.Contains("не настроен", result);
    }

    [Fact]
    public void SummarizeAsync_ReturnsFallback_WhenApiReturnsEmptyChoices()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");

        var json = @"{
            ""choices"": []
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = new Mock<ILogger<ChatService>>().Object;
        var service = new ChatService(httpClient, config.Object, logger);

        // Act
        var result = service.SummarizeAsync("Test article").Result;

        // Assert
        Assert.Contains("недоступна", result);
    }

    [Fact]
    public void SummarizeAsync_ReturnsFallback_OnHttpError()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = new Mock<ILogger<ChatService>>().Object;
        var service = new ChatService(httpClient, config.Object, logger);

        // Act
        var result = service.SummarizeAsync("Test article").Result;

        // Assert
        Assert.Contains("недоступна", result);
    }
}