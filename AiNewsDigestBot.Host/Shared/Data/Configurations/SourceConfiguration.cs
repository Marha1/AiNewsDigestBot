using AiNewsDigestBot.Host.Shared.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiNewsDigestBot.Host.Shared.Data.Configurations;

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Sources");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Url)
            .IsRequired()
            .HasMaxLength(500);

        // Связь (можно оставить, но не обязательно, уже настроена в Article)
        builder.HasMany(s => s.Articles)
            .WithOne(a => a.Source)
            .HasForeignKey(a => a.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}