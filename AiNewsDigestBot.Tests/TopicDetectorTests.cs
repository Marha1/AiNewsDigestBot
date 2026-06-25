using AiNewsDigestBot.Host.Shared.Data.Enums;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;

namespace AiNewsDigestBot.Tests;

/// <summary>
/// Тесты для TopicDetector: определение темы по тексту
/// </summary>
public class TopicDetectorTests
{
    private readonly TopicDetector _detector = new();

    /// <summary>
    /// Проверка: детектор правильно определяет тему по ключевым словам
    /// </summary>
    [Theory]
    [InlineData("Apple announces new iPhone with AI features", Topic.Technology)]
    [InlineData("New AI model GPT-5 released by OpenAI", Topic.ArtificialIntelligence)]
    [InlineData("C# 13 features every developer should know", Topic.Programming)]
    [InlineData("Stock market reaches all-time high", Topic.Business)]
    [InlineData("New vaccine shows 90% effectiveness", Topic.Health)]
    [InlineData("Manchester United wins Premier League", Topic.Sports)]
    [InlineData("President signs new trade agreement", Topic.Politics)]
    [InlineData("New Marvel movie breaks box office records", Topic.Entertainment)]
    public void Detect_ShouldReturnCorrectTopic(string text, Topic expectedTopic)
    {
        var result = _detector.Detect(text);
        Assert.Equal(expectedTopic, result);
    }

    /// <summary>
    /// Проверка: для текста без ключевых слов возвращается null
    /// </summary>
    [Theory]
    [InlineData("Some random text without any keywords")]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_ShouldReturnNullForUnknownTopics(string text)
    {
        var result = _detector.Detect(text);
        Assert.Null(result);
    }

    /// <summary>
    /// Проверка: при совпадении с несколькими темами выбирается наиболее релевантная
    /// </summary>
    [Fact]
    public void Detect_ShouldReturnMostRelevantTopicWhenMultipleMatches()
    {
        var text = "Apple releases new iPhone with AI-powered camera and machine learning features";
        var result = _detector.Detect(text);

        Assert.NotNull(result);
        Assert.Contains(result.Value, new[] { Topic.Technology, Topic.ArtificialIntelligence });
    }
}