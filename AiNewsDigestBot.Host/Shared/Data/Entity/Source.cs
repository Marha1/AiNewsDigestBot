namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class Source
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}