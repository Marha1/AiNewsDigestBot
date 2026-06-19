using Newtonsoft.Json;

namespace AiNewsDigestBot.Host.Shared.Data.Entity.ChatEntity;

public class OpenAiUsage
{
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}