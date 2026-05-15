using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador público do Webriders (homepage, detalhe de post, categoria, pesquisa, newsletter).
/// </summary>
public class HomeController : Controller
{
    private readonly IPostService       _posts;
    private readonly ICategoryService   _categories;
    private readonly ISettingsService   _settings;
    private readonly INewsletterService _newsletter;

    public HomeController(
        IPostService       posts,
        ICategoryService   categories,
        ISettingsService   settings,
        INewsletterService newsletter)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
        _newsletter = newsletter;
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

        // Se não há posts em destaque, usa os 3 mais recentes para o hero
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

        var html = Markdig.Markdown.ToHtml(post.Content ?? "");

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
        // 1. Define o tamanho da página (resolve o erro CS0103)
        const int pageSize = 9;

        // 2. Mapeia para os novos controllers
        string targetController = slug?.ToLower() switch
        {
            "novidades" => "Novidades",
            "rotas" => "Rotas",
            "reviews" => "Reviews",
            "eventos" => "Eventos",
            _ => null
        };

        // 3. Se for uma categoria especial, redireciona para o controller dela
        if (targetController != null)
        {
            return RedirectToAction("Index", targetController, new { page });
        }

        // 4. Se não for especial, busca a categoria genérica
        var cat = await _categories.GetBySlugAsync(slug);
        if (cat is null) return NotFound();

        var total = await _posts.CountPublishedAsync(slug);
        var posts = await _posts.GetPublishedAsync(page, pageSize, slug);

        var vm = new CategoryViewModel
        {
            Category = cat,
            Settings = await _settings.GetAsync(),
            Posts = posts,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalItems = total,
                PageSize = pageSize
            }
        };

        // Certifique-se de que o arquivo Views/Home/Category.cshtml existe!
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

        ViewBag.Query    = q;
        ViewBag.Total    = total;
        ViewBag.Settings = await _settings.GetAsync();
        ViewBag.Posts    = posts;
        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages  = (int)Math.Ceiling((double)total / pageSize),
            TotalItems  = total,
            PageSize    = pageSize
        };
        return View();
    }

    // POST /newsletter
    [HttpPost("newsletter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Newsletter(NewsletterFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["NewsletterError"] = "Preencha nome e e-mail válidos.";
            return RedirectToAction(nameof(Index));
        }
        var ok = await _newsletter.SubscribeAsync(vm.Name, vm.Email);
        TempData[ok ? "NewsletterSuccess" : "NewsletterError"] = ok
            ? "Inscrição realizada! Bem-vindo ao Webriders 🏍️"
            : "Este e-mail já está cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    // GET /erro/{code}
    [Route("erro/{code:int}")]
    public IActionResult Error(int code) => View("Error", code);
}
