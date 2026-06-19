namespace AiNewsDigestBot.Host.Shared.Data.Configurations;

public class TelegramConfig
{
    public const string SectionName = "Telegram";
    public string Token { get; set; } = string.Empty;
}