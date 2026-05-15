using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

// ══════════════════════════════════════════════════════════════
//  Novidades  →  GET /novidades?page=N
// ══════════════════════════════════════════════════════════════
[Route("novidades")]
public class NovidadesController : Controller
{
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;

    public NovidadesController(IPostService posts, ICategoryService categories, ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string slug     = "novidades";
        const int    pageSize = 9;

        var cat = await _categories.GetBySlugAsync(slug);
        if (cat is null) return NotFound();

        var allPosts  = await _posts.GetPublishedAsync(1, int.MaxValue, slug);
        var hero      = allPosts.FirstOrDefault();
        var ticker    = allPosts.Take(6).ToList();
        var total     = await _posts.CountPublishedAsync(slug);
        var pagePosts = await _posts.GetPublishedAsync(page, pageSize, slug);

        var vm = new CategoryPageViewModel
        {
            Category     = cat,
            Settings     = await _settings.GetAsync(),
            HeroPost     = hero,
            TickerPosts  = ticker,
            FeaturedPosts= [],
            Posts        = pagePosts,
            Pagination   = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };

        return View(vm);
    }
}

// ══════════════════════════════════════════════════════════════
//  Rotas  →  GET /rotas?page=N
// ══════════════════════════════════════════════════════════════
[Route("rotas")]
public class RotasController : Controller
{
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;

    public RotasController(IPostService posts, ICategoryService categories, ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string slug     = "rotas";
        const int    pageSize = 9;

        var cat = await _categories.GetBySlugAsync(slug);
        if (cat is null) return NotFound();

        var featured  = await _posts.GetFeaturedByCategoryAsync(slug, 3);
        var total     = await _posts.CountPublishedAsync(slug);
        var pagePosts = await _posts.GetPublishedAsync(page, pageSize, slug);

        // O hero é o post mais recente; remove da lista principal pra não duplicar
        var hero = pagePosts.FirstOrDefault();

        var vm = new CategoryPageViewModel
        {
            Category      = cat,
            Settings      = await _settings.GetAsync(),
            HeroPost      = hero,
            FeaturedPosts = featured,
            TickerPosts   = [],
            Posts         = page == 1 ? pagePosts.Skip(1).ToList() : pagePosts,
            Pagination    = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };

        return View("~/Views/Novidades/Index.cshtml", vm);
    }
}

// ══════════════════════════════════════════════════════════════
//  Reviews  →  GET /reviews?page=N
// ══════════════════════════════════════════════════════════════
[Route("reviews")]
public class ReviewsController : Controller
{
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;

    public ReviewsController(IPostService posts, ICategoryService categories, ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string slug     = "reviews";
        const int    pageSize = 9;

        var cat = await _categories.GetBySlugAsync(slug);
        if (cat is null) return NotFound();

        var featured  = await _posts.GetFeaturedByCategoryAsync(slug, 4);
        var total     = await _posts.CountPublishedAsync(slug);
        var pagePosts = await _posts.GetPublishedAsync(page, pageSize, slug);
        var hero      = pagePosts.FirstOrDefault();

        var vm = new CategoryPageViewModel
        {
            Category      = cat,
            Settings      = await _settings.GetAsync(),
            HeroPost      = hero,
            FeaturedPosts = featured,
            TickerPosts   = [],
            Posts         = page == 1 ? pagePosts.Skip(1).ToList() : pagePosts,
            Pagination    = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };

        return View(vm);
    }
}

// ══════════════════════════════════════════════════════════════
//  Eventos  →  GET /eventos?page=N
// ══════════════════════════════════════════════════════════════
[Route("eventos")]
public class EventosController : Controller
{
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;

    public EventosController(IPostService posts, ICategoryService categories, ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string slug     = "eventos";
        const int    pageSize = 9;

        var cat = await _categories.GetBySlugAsync(slug);
        if (cat is null) return NotFound();

        // "Próximos eventos" = posts mais recentes com IsFeatured
        var upcoming  = await _posts.GetFeaturedByCategoryAsync(slug, 3);
        var total     = await _posts.CountPublishedAsync(slug);
        var pagePosts = await _posts.GetPublishedAsync(page, pageSize, slug);
        var hero      = pagePosts.FirstOrDefault();

        var vm = new CategoryPageViewModel
        {
            Category      = cat,
            Settings      = await _settings.GetAsync(),
            HeroPost      = hero,
            FeaturedPosts = upcoming,
            TickerPosts   = [],
            Posts         = page == 1 ? pagePosts.Skip(1).ToList() : pagePosts,
            Pagination    = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };

        return View(vm);
    }
}
