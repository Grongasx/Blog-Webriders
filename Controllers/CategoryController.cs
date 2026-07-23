using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador dinâmico para todas as páginas de categoria públicas.
/// Aceita qualquer slug cadastrado no banco — sem hardcode.
/// </summary>
[Route("{categorySlug}")]
public class CategoryController : Controller
{    
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;

    public CategoryController(
        IPostService     posts,
        ICategoryService categories,
        ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    /// <summary>
    /// GET /{categorySlug}?page=N
    /// Funciona para qualquer categoria cadastrada no banco.
    /// </summary>
    [HttpGet("", Name = "CategoryPage")]
    public async Task<IActionResult> Index([FromRoute] string categorySlug, [FromQuery] int page = 1)
    {
        const int pageSize = 9;
        categorySlug = categorySlug.ToLower().Trim();

        // 1. Valida se a categoria existe no banco
        var cat = await _categories.GetBySlugAsync(categorySlug);
        if (cat is null) return NotFound();

        // 2. Comportamento visual por slug — extensível sem alterar o controller:
        //    basta adicionar o slug novo aqui se quiser comportamento especial,
        //    caso contrário usa o padrão (0, 0, false)
        var (featuredCount, tickerCount, shouldSkipFirst) = categorySlug switch
        {
            "novidades" => (0, 6, false),
            "rotas"     => (3, 0, true),
            "reviews"   => (4, 0, true),
            "eventos"   => (3, 0, true),
            _           => (0, 0, false)   // ← novas categorias caem aqui automaticamente
        };

        // 3. Busca de dados
        var total     = await _posts.CountPublishedAsync(categorySlug);
        var pagePosts = await _posts.GetPublishedAsync(page, pageSize, categorySlug);

        var hero     = pagePosts.FirstOrDefault();
        var featured = featuredCount > 0
            ? await _posts.GetFeaturedByCategoryAsync(categorySlug, featuredCount)
            : (IReadOnlyList<ThrottleBlog.Models.Post>)[];
        var ticker   = tickerCount > 0
            ? (await _posts.GetPublishedAsync(1, tickerCount, categorySlug)).ToList()
            : [];

        // 4. ViewModel
        var vm = new CategoryPageViewModel
        {
            Category      = cat,
            Settings      = await _settings.GetAsync(),
            HeroPost      = hero,
            FeaturedPosts = featured,
            TickerPosts   = ticker,
            Posts         = (page == 1 && shouldSkipFirst)
                                ? pagePosts.Skip(1).ToList()
                                : pagePosts,
            Pagination    = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };

        // 5. View: Views/Shared/Index.cshtml (compartilhada por todas as categorias)
        return View("~/Views/Shared/Index.cshtml", vm);
    }
}
