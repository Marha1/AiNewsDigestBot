namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class NewsApiArticle
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public DateTime? PublishedAt { get; set; }
}