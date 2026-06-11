namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class User
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public UserSettings? Settings { get; set; }
}