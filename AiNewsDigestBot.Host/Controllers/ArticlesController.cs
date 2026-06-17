using AiNewsDigestBot.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiNewsDigestBot.Host.Controllers;

[ApiController]
[Route("articles")]
public class ArticlesController : ControllerBase
{
    private readonly ArticleService _articleService;

    public ArticlesController(ArticleService articleService)
    {
        _articleService = articleService;
    }

    /// <summary>
    ///     Получить последние статьи с пагинацией
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1)</param>
    /// <param name="pageSize">Количество статей на странице (1-50, по умолчанию 10)</param>
    [HttpGet]
    public async Task<IActionResult> Get(
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _articleService
            .GetLatestArticlesPagedAsync(page, pageSize);

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        string query,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _articleService
            .SearchArticlesPagedAsync(query, page, pageSize);

        return Ok(result);
    }
}