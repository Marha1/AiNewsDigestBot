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
    /// Получить все активные источники, отсортированные по приоритету
    /// </summary>
    public async Task<List<Source>> GetActiveSourcesAsync()
    {
        return await _db.Sources
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// Получить источник по ID
    /// </summary>
    public async Task<Source?> GetSourceByIdAsync(Guid id)
    {
        return await _db.Sources.FindAsync(id);
    }

   

    /// <summary>
    /// Добавить новый источник
    /// </summary>
    public async Task AddSourceAsync(Source source)
    {
        _db.Sources.Add(source);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Обновить источник
    /// </summary>
    public async Task UpdateSourceAsync(Source source)
    {
        _db.Sources.Update(source);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Включить/выключить источник
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