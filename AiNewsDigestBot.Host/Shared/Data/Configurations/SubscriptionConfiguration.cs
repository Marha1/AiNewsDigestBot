using AiNewsDigestBot.Host.Shared.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiNewsDigestBot.Host.Shared.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.UserId, s.Topic })
            .IsUnique();

        builder.HasIndex(s => s.Topic);

        builder.Property(s => s.Topic)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.SubscribedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}