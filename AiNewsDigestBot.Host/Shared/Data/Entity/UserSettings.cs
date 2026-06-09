namespace AiNewsDigestBot.Host.Shared.Data.Entity;

public class UserSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int DigestHour { get; set; } = 9;      
    public int DigestMinute { get; set; } = 0;    
    public int ArticlesPerDigest { get; set; } = 10;  
    public bool IsEnabled { get; set; } = true;   
}