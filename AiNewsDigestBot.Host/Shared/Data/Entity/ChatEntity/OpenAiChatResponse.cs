using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;

public class OpenAiChatResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("object")]
    public string? Object { get; set; }

    [JsonProperty("created")]
    public long Created { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("choices")]
    public List<OpenAiChoice>? Choices { get; set; }

    [JsonProperty("usage")]
    public OpenAiUsage? Usage { get; set; }

    [JsonProperty("error")]
    public OpenAiError? Error { get; set; }
}