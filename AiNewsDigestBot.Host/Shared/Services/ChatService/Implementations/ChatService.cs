using System.Text;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Services.ChatService.Implementations;

public class ChatService : IChatService
{
    private readonly string? _Apikey;
    private readonly HttpClient _httpClient;


    public ChatService(HttpClient client, IConfiguration config)
    {
        _httpClient = client;
        _Apikey = config["OpenAI:ApiKey"];
    }

    public async Task<string> SummarizeAsync(string articleText)
    {
        // 1. Формируем правильный запрос для chat/completions
        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "Ты — помощник, который кратко пересказывает новости. Отвечай только 2-3 предложениями на русском языке."
                },
                new
                {
                    role = "user",
                    content = $"Суммаризируй этот текст: {articleText}"
                }
            },
            max_tokens = 150,
            temperature = 0.5
        };

        // 2. Отправляем запрос с авторизацией
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_Apikey}");
        httpRequest.Content = new StringContent(
            JsonConvert.SerializeObject(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Ошибка {response.StatusCode}: {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonConvert.DeserializeObject(content);

        // Извлекаем текст ответа
        return result?.choices[0]?.message?.content ?? "Не удалось суммаризировать текст";
    }
}