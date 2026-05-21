using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador público do Webriders (homepage, detalhe de post, categoria, pesquisa, newsletter).
/// </summary>
public class HomeController : Controller
{
    private readonly IPostService           _posts;
    private readonly ICategoryService       _categories;
    private readonly ISettingsService       _settings;
    private readonly INewsletterService     _newsletter;
    private readonly IMarkdownRenderer      _markdown;
    private readonly IProductBlockRenderer  _productRenderer; // ← NOVO

    public HomeController(
        IPostService            posts,
        ICategoryService        categories,
        ISettingsService        settings,
        INewsletterService      newsletter,
        IMarkdownRenderer       markdown,
        IProductBlockRenderer   productRenderer) // ← NOVO
    {
        _posts           = posts;
        _categories      = categories;
        _settings        = settings;
        _newsletter      = newsletter;
        _markdown        = markdown;
        _productRenderer = productRenderer; // ← NOVO
    }

    // GET /
    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            Settings      = await _settings.GetAsync(),
            FeaturedPosts = await _posts.GetFeaturedAsync(3),
            LatestPosts   = await _posts.GetLatestAsync(6),
            MostRead      = await _posts.GetMostReadAsync(3),
            Categories    = await _categories.GetAllAsync(),
        };

        if (!vm.FeaturedPosts.Any())
            vm.FeaturedPosts = vm.LatestPosts.Take(3).ToList();

        return View(vm);
    }

    // GET /post/{slug}
    [Route("post/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var post = await _posts.GetBySlugAsync(slug);
        if (post is null) return NotFound();

        // ── CORREÇÃO: expande :::produto URL ::: → HTML ANTES do Markdown render ──
        var processedContent = await _productRenderer.ExpandProductBlocksAsync(post.Content);
        var html = _markdown.ToHtml(processedContent);
        // ─────────────────────────────────────────────────────────────────────────

        var vm = new PostDetailViewModel
        {
            Post         = post,
            Settings     = await _settings.GetAsync(),
            ContentHtml  = html,
            RelatedPosts = await _posts.GetRelatedAsync(post.Id, post.CategoryId),
            MostRead     = await _posts.GetMostReadAsync(5),
        };
        return View(vm);
    }

    // GET /categoria/{slug}?page=1
    [Route("categoria/{slug}")]
    public async Task<IActionResult> Category(string slug, int page = 1)
    {
        const int pageSize = 9;

        string? targetController = slug?.ToLower() switch
        {
            "novidades" => "Novidades",
            "rotas"     => "Rotas",
            "reviews"   => "Reviews",
            "eventos"   => "Eventos",
            _           => null
        };

        if (targetController != null)
            return RedirectToAction("Index", targetController, new { page });

        var cat = await _categories.GetBySlugAsync(slug!);
        if (cat is null) return NotFound();

        var total = await _posts.CountPublishedAsync(slug);
        var posts = await _posts.GetPublishedAsync(page, 10, slug);

        var vm = new CategoryViewModel
        {
            Category   = cat,
            Settings   = await _settings.GetAsync(),
            Posts      = posts,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };
        return View(vm);
    }

    // GET /buscar?q=honda&page=1
    [Route("buscar")]
    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        const int pageSize = 9;
        if (string.IsNullOrWhiteSpace(q))
            return RedirectToAction(nameof(Index));

        var total = await _posts.CountPublishedAsync(search: q);
        var posts = await _posts.GetPublishedAsync(page, pageSize, search: q);

        var vm = new SearchViewModel
        {
            Query      = q.Trim(),
            Posts      = posts,
            Total      = total,
            Settings   = await _settings.GetAsync(),
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };
        return View(vm);
    }

    // GET /newsletter
    [Route("newsletter")]
    public async Task<IActionResult> NewsletterPage()
    {
        var settings = await _settings.GetAsync();
        var counts   = await _newsletter.CountByTierAsync();
        return View("Newsletter", new NewsletterPageViewModel
        {
            Settings     = settings,
            CommonCount  = counts.Common,
            PremiumCount = counts.Premium
        });
    }

    // POST /newsletter (assinatura comum)
    [HttpPost("newsletter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Newsletter(NewsletterFormViewModel vm, string? returnTo = null)
    {
        var isAjax  = IsAjaxRequest();
        var redirect = returnTo == "Index" ? nameof(Index) : nameof(NewsletterPage);
        if (!ModelState.IsValid)
            return NewsletterFeedback(isAjax, false, "Preencha nome e e-mail válidos.", redirect);

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, NewsletterTier.Common);
        return NewsletterFeedback(isAjax, result.Success, result.Message, redirect);
    }

    // POST /newsletter/premium
    [HttpPost("newsletter/premium")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewsletterPremium(PremiumNewsletterFormViewModel vm)
    {
        var isAjax = IsAjaxRequest();
        if (!ModelState.IsValid)
            return NewsletterFeedback(isAjax, false, "Preencha todos os campos corretamente.", nameof(NewsletterPage));

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, NewsletterTier.Premium, vm.Phone);
        return NewsletterFeedback(isAjax, result.Success, result.Message, nameof(NewsletterPage));
    }

    private bool IsAjaxRequest()
        => string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

    private IActionResult NewsletterFeedback(bool isAjax, bool success, string message, string redirectAction = nameof(NewsletterPage))
    {
        if (isAjax) return Json(new { success, message });
        TempData[success ? "NewsletterSuccess" : "NewsletterError"] = message;
        return RedirectToAction(redirectAction);
    }

    // GET /erro/{code}
    [Route("erro/{code:int}")]
    public IActionResult Error(int code) => View("Error", code);
}
