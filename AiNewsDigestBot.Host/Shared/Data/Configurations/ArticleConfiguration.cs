using AiNewsDigestBot.Host.Shared.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.Url).IsUnique();

        // Настройка связи с Source
        builder.HasOne(a => a.Source)
            .WithMany(s => s.Articles)
            .HasForeignKey(a => a.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Остальные свойства
        builder.Property(a => a.Title).IsRequired().HasColumnType("text");
        builder.Property(a => a.Url).IsRequired().HasColumnType("text");
        builder.Property(a => a.Description).HasColumnType("text");
        builder.Property(a => a.Summary).HasColumnType("text");
        builder.Property(a => a.PublishedAt).IsRequired();
        builder.Property(a => a.ParsedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Индексы
        builder.HasIndex(a => a.PublishedAt);
    }
}