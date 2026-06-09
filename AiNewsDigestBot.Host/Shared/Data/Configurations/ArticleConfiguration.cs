using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiNewsDigestBot.Host.Shared.Data.Entity;

namespace AiNewsDigestBot.Host.Shared.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        
        builder.HasKey(a => a.Id);
        
        builder.HasIndex(a => a.Url)
            .IsUnique();
        
        builder.HasIndex(a => a.Source);
        builder.HasIndex(a => a.PublishedAt);
        
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(a => a.Url)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(a => a.Description)
            .HasMaxLength(2000);
        
        builder.Property(a => a.Summary)
            .HasMaxLength(1000);
        
        builder.Property(a => a.Source)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(a => a.PublishedAt)
            .IsRequired();
        
        builder.Property(a => a.ParsedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}