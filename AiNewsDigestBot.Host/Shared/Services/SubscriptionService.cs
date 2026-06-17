using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class SubscriptionService
{
    private readonly AppDbContext _db;

    public SubscriptionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Subscription>> GetUserSubscriptionsAsync(Guid userId)
    {
        return await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }
    public async Task<bool> IsUserSubscribedAsync(Guid userId, Topic topic)
    {
        return await _db.Subscriptions
            .AnyAsync(s => s.UserId == userId && s.Topic == topic);
    }

    public async Task<Subscription> AddSubscriptionAsync(Guid userId, Topic topic)
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Topic = topic,
            SubscribedAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();
        return subscription;
    }
    public async Task<bool> RemoveSubscriptionAsync(Guid userId, Topic topic)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Topic == topic);
    
        if (subscription == null)
            return false;

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<List<Topic>> GetUserSubscriptionTopicsAsync(Guid userId)
    {
        return await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .Select(s => s.Topic)
            .ToListAsync();
    }
}