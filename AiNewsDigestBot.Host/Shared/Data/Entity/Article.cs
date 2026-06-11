namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class Article
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime ParsedAt { get; set; }
    public Guid SourceId { get; set; }
    public Source Source { get; set; } = null!;
}