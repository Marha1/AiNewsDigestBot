using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;

public  class OpenAiError
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }
}