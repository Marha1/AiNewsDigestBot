namespace AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;

public interface IChatService
{
    Task<string> SummarizeAsync(string articleText);
}