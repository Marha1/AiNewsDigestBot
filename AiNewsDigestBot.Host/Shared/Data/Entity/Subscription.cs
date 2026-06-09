using AiNewsDigestBot.Host.Shared.Data.Enum;

namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null;
    public Topic Topic { get; set; } 
    public DateTime SubscribedAt { get; set; }
}