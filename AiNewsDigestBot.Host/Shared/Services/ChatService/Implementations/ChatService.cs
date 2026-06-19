using System.Text;
using AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Services.ChatService.Implementations;

public class ChatService : IChatService
{
    private readonly string? _apiKey;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(HttpClient client, IConfiguration config, ILogger<ChatService> logger)
    {
        _httpClient = client;
        _apiKey = config["OpenAI:ApiKey"];
        _logger = logger;
    }

    public async Task<string> SummarizeAsync(string articleText)
    {
        // Если ключ не настроен — возвращаем заглушку
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured");
            return "⚠️ Суммаризация недоступна (ключ API не настроен)";
        }

        // Ограничиваем длину текста (чтобы не превысить лимиты токенов)
        var trimmedText = articleText.Length > 2000 
            ? articleText[..2000] + "..." 
            : articleText;

        try
        {
            // 1. Формируем запрос
            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Ты — помощник, который кратко пересказывает новости. Отвечай только 2-3 предложениями на русском языке."
                    },
                    new
                    {
                        role = "user",
                        content = $"Суммаризируй этот текст: {trimmedText}"
                    }
                },
                max_tokens = 150,
                temperature = 0.5
            };

            // 2. Отправляем запрос
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            httpRequest.Content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, error);
                return "⚠️ Суммаризация временно недоступна";
            }

            var content = await response.Content.ReadAsStringAsync();
            
            // 3. Разбираем ответ в типизированный класс
            var openAiResponse = JsonConvert.DeserializeObject<OpenAiChatResponse>(content);
            
            // 4. Проверяем, что ответ валидный
            if (openAiResponse?.Choices == null || openAiResponse.Choices.Count == 0)
            {
                _logger.LogWarning("OpenAI API returned empty response");
                return "⚠️ Суммаризация недоступна";
            }

            var summary = openAiResponse.Choices[0].Message?.Content?.Trim();
            
            if (string.IsNullOrWhiteSpace(summary))
            {
                _logger.LogWarning("OpenAI API returned empty summary");
                return "⚠️ Суммаризация недоступна";
            }

            return summary;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling OpenAI API");
            return "⚠️ Суммаризация временно недоступна (ошибка сети)";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from OpenAI API");
            return "⚠️ Суммаризация временно недоступна (ошибка обработки)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while summarizing");
            return "⚠️ Суммаризация временно недоступна";
        }
    }
}



