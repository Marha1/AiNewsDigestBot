using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace AiNewsDigestBot.Host.Shared.Services;

public class SourceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SourceService> _logger;

    public SourceService(AppDbContext db, ILogger<SourceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    ///     Получить все активные источники
    /// </summary>
    public async Task<List<Source>> GetActiveSourcesAsync()
    {
        var sources = await _db.Sources
            .Where(s => s.IsActive)
            .ToListAsync();

        if (sources.Count == 0)
        {
            _logger.LogWarning("No sources found. Adding default sources...");
            await AddDefaultSourcesAsync();


            sources = await _db.Sources
                .Where(s => s.IsActive)
                .ToListAsync();
        }

        return sources;
    }

    /// <summary>
    ///     Получить источник по ID
    /// </summary>
    public async Task<Source?> GetSourceByIdAsync(Guid id)
    {
        return await _db.Sources.FindAsync(id);
    }

    // <summary>
    /// Добавить дефолтные источники (вызывается автоматически, если таблица пуста)
    /// </summary>
    public async Task AddDefaultSourcesAsync()
    {
        var defaultSources = new List<Source>
        {
            // Технологии и IT
            new() { Id = Guid.NewGuid(), Url = "https://techcrunch.com/feed/", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.theverge.com/rss/index.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://habr.com/ru/rss/all/", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://github.blog/feed/", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://stackoverflow.blog/feed/", IsActive = true },

            // AI и программирование
            new() { Id = Guid.NewGuid(), Url = "https://venturebeat.com/category/ai/feed/", IsActive = true },
            new()
            {
                Id = Guid.NewGuid(), Url = "https://habr.com/ru/hubs/artificial_intelligence/rss/articles/",
                IsActive = true
            },
            new() { Id = Guid.NewGuid(), Url = "https://habr.com/ru/hubs/programming/rss/articles/", IsActive = true },

            // Наука
            new() { Id = Guid.NewGuid(), Url = "https://naked-science.ru/feed", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.sciencedaily.com/rss/all.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://phys.org/rss-feed/", IsActive = true },

            // Бизнес и экономика
            new()
            {
                Id = Guid.NewGuid(), Url = "https://rssexport.rbc.ru/rbcnews/economics/30/full.rss", IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(), Url = "https://rssexport.rbc.ru/rbcnews/business/30/full.rss", IsActive = true
            },

            // Здоровье
            new() { Id = Guid.NewGuid(), Url = "https://medportal.ru/rss/", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.medicalnewstoday.com/featured.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.who.int/rss-feeds/news-stand-rss.xml", IsActive = true },

            // Спорт
            new() { Id = Guid.NewGuid(), Url = "https://www.championat.com/rss/news.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://feeds.bbci.co.uk/sport/rss.xml", IsActive = true },

            // Развлечения и игры
            new() { Id = Guid.NewGuid(), Url = "https://variety.com/feed/", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.igromania.ru/rss/news.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.ign.com/rss", IsActive = true },

            // Безопасность
            new() { Id = Guid.NewGuid(), Url = "https://www.securitylab.ru/_services/rss.asp", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://www.bleepingcomputer.com/feed/", IsActive = true },

            // Политика
            new() { Id = Guid.NewGuid(), Url = "https://ria.ru/export/rss2/politics/index.xml", IsActive = true },
            new() { Id = Guid.NewGuid(), Url = "https://feeds.bbci.co.uk/news/politics/rss.xml", IsActive = true }
        };

        await _db.Sources.AddRangeAsync(defaultSources);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Added {Count} default sources", defaultSources.Count);
    }


    /// <summary>
    ///     Добавить новый источник
    /// </summary>
    public async Task AddSourceAsync(Source source)
    {
        _db.Sources.Add(source);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    ///     Обновить источник
    /// </summary>
    public async Task UpdateSourceAsync(Source source)
    {
        _db.Sources.Update(source);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    ///     Включить/выключить источник
    /// </summary>
    public async Task ToggleSourceStatusAsync(Guid id, bool isActive)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source != null)
        {
            source.IsActive = isActive;
            await _db.SaveChangesAsync();
        }
    }
}