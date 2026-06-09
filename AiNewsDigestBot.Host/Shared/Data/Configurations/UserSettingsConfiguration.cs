using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiNewsDigestBot.Host.Shared.Data.Entity;

namespace AiNewsDigestBot.Host.Shared.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        
        builder.HasKey(us => us.Id);
        
        builder.HasIndex(us => us.UserId)
            .IsUnique();
        
        builder.Property(us => us.DigestHour)
            .IsRequired()
            .HasDefaultValue(9);
        
        builder.Property(us => us.DigestMinute)
            .IsRequired()
            .HasDefaultValue(0);
        
        builder.Property(us => us.ArticlesPerDigest)
            .IsRequired()
            .HasDefaultValue(10);
        
        builder.Property(us => us.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);
    }
}