using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Entity;
using AiNewsDigestBot.Host.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AiNewsDigestBot.Tests.Services;

/// <summary>
/// Тесты для SourceService: работа с источниками новостей
/// </summary>
public class SourceServiceTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private SourceService CreateService(AppDbContext db)
    {
        var logger = new Mock<ILogger<SourceService>>().Object;
        return new SourceService(db, logger);
    }

    [Fact]
    public async Task GetActiveSourcesAsync_ReturnsOnlyActiveSources()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        db.Sources.Add(new Source { Id = Guid.NewGuid(), Url = "https://active1.com", IsActive = true });
        db.Sources.Add(new Source { Id = Guid.NewGuid(), Url = "https://active2.com", IsActive = true });
        db.Sources.Add(new Source { Id = Guid.NewGuid(), Url = "https://inactive.com", IsActive = false });
        await db.SaveChangesAsync();

        var result = await service.GetActiveSourcesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task GetActiveSourcesAsync_AddsDefaultSources_WhenNoSourcesExist()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        // База пустая, источников нет
        var result = await service.GetActiveSourcesAsync();

        // Проверяем, что добавились дефолтные источники
        Assert.NotEmpty(result);
        Assert.All(result, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task AddSourceAsync_AddsNewSource()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Url = "https://news.com",
            IsActive = true
        };

        await service.AddSourceAsync(source);

        var saved = await db.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);
        Assert.NotNull(saved);
        Assert.Equal("https://news.com", saved.Url);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task GetSourceByIdAsync_ReturnsCorrectSource()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var source = new Source { Id = Guid.NewGuid(), Url = "https://test.com", IsActive = true };
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var result = await service.GetSourceByIdAsync(source.Id);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result.Id);
        Assert.Equal("https://test.com", result.Url);
    }

    [Fact]
    public async Task GetSourceByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetSourceByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateSourceAsync_UpdatesExistingSource()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var source = new Source { Id = Guid.NewGuid(), Url = "https://old.com", IsActive = true };
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        source.Url = "https://updated.com";
        await service.UpdateSourceAsync(source);

        var updated = await db.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);
        Assert.NotNull(updated);
        Assert.Equal("https://updated.com", updated.Url);
    }

    [Fact]
    public async Task ToggleSourceStatusAsync_TogglesIsActive()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var source = new Source { Id = Guid.NewGuid(), Url = "https://test.com", IsActive = true };
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        // Выключаем
        await service.ToggleSourceStatusAsync(source.Id, false);
        var disabled = await db.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);
        Assert.False(disabled.IsActive);

        // Включаем
        await service.ToggleSourceStatusAsync(source.Id, true);
        var enabled = await db.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);
        Assert.True(enabled.IsActive);
    }

    [Fact]
    public async Task AddDefaultSourcesAsync_AddsAllDefaultSources()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Проверяем, что база пустая
        var before = await db.Sources.CountAsync();
        Assert.Equal(0, before);

        // Добавляем дефолтные источники
        await service.AddDefaultSourcesAsync();

        // Проверяем, что они добавились
        var after = await db.Sources.CountAsync();
        Assert.Equal(25, after); // количество источников в методе AddDefaultSourcesAsync
    }
}