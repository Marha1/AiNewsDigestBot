namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class Source
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}