using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;

public  class OpenAiMessage
{
    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }
}