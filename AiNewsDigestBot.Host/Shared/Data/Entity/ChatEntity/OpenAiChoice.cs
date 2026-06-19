using Newtonsoft.Json;
namespace AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;

public class OpenAiChoice
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public OpenAiMessage? Message { get; set; }

    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}