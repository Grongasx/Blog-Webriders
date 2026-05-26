using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Painel de administração — todas as rotas exigem autenticação (role Admin).
/// </summary>
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IPostService _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService _settings;
    private readonly IDashboardService _dashboard;
    private readonly IImageUploadService _uploads;
    private readonly INewsletterService _newsletter;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(
        IPostService posts,
        ICategoryService categories,
        ISettingsService settings,
        IDashboardService dashboard,
        IImageUploadService uploads,
        INewsletterService newsletter,
        UserManager<ApplicationUser> users)
    {
        _posts = posts;
        _categories = categories;
        _settings = settings;
        _dashboard = dashboard;
        _uploads = uploads;
        _newsletter = newsletter;
        _users = users;
    }

    // ══════════════════════════════════════════════
    //  DASHBOARD  GET /admin
    // ══════════════════════════════════════════════
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        var vm = await _dashboard.GetDashboardAsync();
        return View(vm);
    }

    // ══════════════════════════════════════════════
    //  POSTS  GET /admin/posts
    // ══════════════════════════════════════════════
    [HttpGet("posts")]
    public async Task<IActionResult> Posts(
        string? q, string? category, string? status, int page = 1)
    {
        const int pageSize = 8;
        var total = await _posts.AdminCountAsync(q, category, status);
        var list = await _posts.AdminListAsync(page, pageSize, q, category, status);

        var vm = new PostListViewModel
        {
            Posts = list,
            Categories = await _categories.GetAllAsync(),
            SearchQuery = q,
            CategoryFilter = category,
            StatusFilter = status,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalItems = total,
                PageSize = pageSize
            }
        };
        return View(vm);
    }

    // ══════════════════════════════════════════════
    //  NOVO POST POST /admin/posts/novo
    // ══════════════════════════════════════════════
    [HttpGet("posts/novo")]
    public async Task<IActionResult> NewPost()
    {
        var vm = new PostFormViewModel
        {
            Status = PostStatus.Draft,
            Categories = await _categories.GetAllAsync() // Carrega a lista aqui
        };
        return View("PostForm", vm);
    }

    // ══════════════════════════════════════════════
    //  Editar POST POST /admin/posts/{id}/editar
    // ══════════════════════════════════════════════

    [HttpGet("posts/{id:int}/editar")]
    public async Task<IActionResult> EditPost(int id)
    {
        var post = await _posts.GetByIdAsync(id);
        if (post is null) return NotFound();

        var vm = new PostFormViewModel
        {
            Id            = post.Id,
            Title         = post.Title,
            Excerpt       = post.Excerpt,
            Content       = post.Content,
            FeaturedImage = post.FeaturedImage,
            ReadingTime   = post.ReadingTime,
            Status        = post.Status,
            IsFeatured    = post.IsFeatured,
            CategoryId    = post.CategoryId,
            Categories    = await _categories.GetAllAsync() // Carrega a lista aqui também
        };
        return View("PostForm", vm);
    }

    // ══════════════════════════════════════════════
    //  SALVAR POST  POST /admin/posts/salvar
    // ══════════════════════════════════════════════
    [HttpPost("posts/salvar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePost(PostFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.GetAllAsync();
            return View("PostForm", vm);
        }

        if (vm.Id == 0)
        {
            var userId = _users.GetUserId(User)!;
            await _posts.CreateAsync(vm, userId);
            TempData["Success"] = "Post criado com sucesso!";
        }
        else
        {
            await _posts.UpdateAsync(vm);
            TempData["Success"] = "Post atualizado!";
        }
        return RedirectToAction(nameof(Posts));
    }

    // ══════════════════════════════════════════════
    //  PUBLICAR RÁPIDO  POST /admin/posts/{id}/publicar
    // ══════════════════════════════════════════════
    [HttpPost("posts/{id:int}/publicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var post = await _posts.GetByIdAsync(id);
        if (post is null) return NotFound();
        if (string.IsNullOrWhiteSpace(post.FeaturedImage))
        {
            TempData["Error"] = "Imagem de destaque é obrigatória para publicar.";
            return RedirectToAction(nameof(EditPost), new { id });
        }
        await _posts.PublishAsync(id);
        TempData["Success"] = "Post publicado!";
        return RedirectToAction(nameof(Posts));
    }

    // ══════════════════════════════════════════════
    //  UPLOAD DE MÍDIA  POST /admin/media/upload
    // ══════════════════════════════════════════════
    [HttpPost("media/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado." });

        try
        {
            var userId = _users.GetUserId(User)!;
            var url = await _uploads.UploadAsync(file, userId);
            return Json(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ══════════════════════════════════════════════
    //  SCRAPER DE PRODUTOS  GET /admin/scrape-product
    // ══════════════════════════════════════════════
    [HttpGet("scrape-product")]
    public async Task<IActionResult> ScrapeProduct([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("webriders.com.br"))
        {
            return BadRequest(new { error = "URL inválida ou não pertence à Webriders." });
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var html = await client.GetStringAsync(url);

            // Mapeia os dados estruturados do produto no HTML (Equivalente semântico de caminhos XPath diretos)
            string ExtractMeta(string property)
            {
                var match = System.Text.RegularExpressions.Regex.Match(html, $"<meta[^>]*property=[\"']{property}[\"'][^>]*content=[\"']([^\"']*)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!match.Success) match = System.Text.RegularExpressions.Regex.Match(html, $"<meta[^>]*content=[\"']([^\"']*)[\"'][^>]*property=[\"']{property}[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return match.Success ? System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value) : string.Empty;
            }

            string title = ExtractMeta("og:title");
            if (string.IsNullOrWhiteSpace(title))
            {
                var titleMatch = System.Text.RegularExpressions.Regex.Match(html, "<h1[^>]*>([^<]*)</h1>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Produto Webriders";
            }

            string image = ExtractMeta("og:image");
            string price = ExtractMeta("product:price:amount");

            if (string.IsNullOrWhiteSpace(price))
            {
                var priceMatch = System.Text.RegularExpressions.Regex.Match(html, @"(?:class|id)=.*price.*?>\s*(?:R\$\s*)?([0-9.,]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                price = priceMatch.Success ? "R$ " + priceMatch.Groups[1].Value.Trim() : "Consultar valor";
            }
            else
            {
                price = "R$ " + price;
            }

            // Captura de atributos/variações comerciais vinculadas no seletor de opções (Tamanho, Cor, etc)
            var variations = new List<string>();
            var variantMatches = System.Text.RegularExpressions.Regex.Matches(html, @"<option[^>]*>([^<]+)</option>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in variantMatches)
            {
                var txt = match.Groups[1].Value.Trim();
                if (!txt.Contains("Selecione") && !txt.Contains("escolha") && txt.Length < 30 && variations.Count < 5)
                {
                    variations.Add(txt);
                }
            }

            if (!variations.Any())
            {
                variations.Add("Tamanho Único");
            }

            return Json(new
            {
                title,
                image = !string.IsNullOrWhiteSpace(image) ? image : "/admin/img/product-placeholder.jpg",
                price,
                variations,
                url
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Falha ao processar estrutura do produto: " + ex.Message });
        }
    }

    // POST /admin/posts/{id}/despublicar
    [HttpPost("posts/{id:int}/despublicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        await _posts.UnpublishAsync(id);
        TempData["Success"] = "Post movido para rascunho.";
        return RedirectToAction(nameof(Posts));
    }

    // ══════════════════════════════════════════════
    //  EXCLUIR POST  POST /admin/posts/{id}/excluir
    // ══════════════════════════════════════════════
    [HttpPost("posts/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        await _posts.DeleteAsync(id);
        TempData["Success"] = "Post excluído.";
        return RedirectToAction(nameof(Posts));
    }

    // ══════════════════════════════════════════════
    //  CATEGORIAS  GET /admin/categorias
    // ══════════════════════════════════════════════
    [HttpGet("categorias")]
    public async Task<IActionResult> Categories()
    {
        var cats = await _categories.GetAllAsync();
        var allPosts = await _posts.AdminListAsync(1, int.MaxValue, null, null, null);

        var vm = cats.Select(c => new CategoryFormViewModel
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = c.ImageUrl,
            SortOrder = c.SortOrder,
            PostCount = allPosts.Count(p => p.CategoryId == c.Id)
        }).ToList();

        return View(vm);
    }

    // GET /admin/categorias/nova
    [HttpGet("categorias/nova")]
    public IActionResult NewCategory() => View("CategoryForm", new CategoryFormViewModel());

    // GET /admin/categorias/{id}/editar
    [HttpGet("categorias/{id:int}/editar")]
    public async Task<IActionResult> EditCategory(int id)
    {
        var cat = await _categories.GetByIdAsync(id);
        if (cat is null) return NotFound();
        return View("CategoryForm", new CategoryFormViewModel
        {
            Id = cat.Id,
            Name = cat.Name,
            ImageUrl = cat.ImageUrl,
            SortOrder = cat.SortOrder
        });
    }

    // POST /admin/categorias/salvar
    [HttpPost("categorias/salvar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("CategoryForm", vm);

        if (vm.Id == 0) await _categories.CreateAsync(vm);
        else await _categories.UpdateAsync(vm);

        TempData["Success"] = vm.Id == 0 ? "Categoria criada!" : "Categoria atualizada!";
        return RedirectToAction(nameof(Categories));
    }

    // POST /admin/categorias/{id}/excluir
    [HttpPost("categorias/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categories.DeleteAsync(id);
        TempData["Success"] = "Categoria removida.";
        return RedirectToAction(nameof(Categories));
    }
    // ══════════════════════════════════════════════
    //  NEWSLETTER  GET /admin/newsletter
    // ══════════════════════════════════════════════
    [HttpGet("newsletter")]
    public async Task<IActionResult> NewsletterSubscribers(
        string? q, string? tier, string? status, int page = 1)
    {
        const int pageSize = 15;
        var (items, total) = await _newsletter.ListAsync(page, pageSize, q, tier, status);
        var counts = await _newsletter.CountByTierAsync();

        var vm = new AdminNewsletterListViewModel
        {
            Subscribers = items,
            SearchQuery = q,
            TierFilter = tier,
            StatusFilter = status,
            CommonTotal = counts.Common,
            PremiumTotal = counts.Premium,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalItems = total,
                PageSize = pageSize
            }
        };
        return View("Newsletter", vm);
    }

    [HttpPost("newsletter/{id:int}/desativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateSubscriber(int id)
    {
        await _newsletter.SetActiveAsync(id, false);
        TempData["Success"] = "Assinante desativado.";
        return RedirectToAction(nameof(NewsletterSubscribers));
    }

    [HttpPost("newsletter/{id:int}/reativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateSubscriber(int id)
    {
        await _newsletter.SetActiveAsync(id, true);
        TempData["Success"] = "Assinante reativado.";
        return RedirectToAction(nameof(NewsletterSubscribers));
    }

    // ══════════════════════════════════════════════
    //  CONFIGURAÇÕES  GET /admin/configuracoes
    // ══════════════════════════════════════════════
    [HttpGet("configuracoes")]
    public async Task<IActionResult> Settings()
    {
        var s = await _settings.GetAsync();
        var vm = new SettingsViewModel
        {
            BlogName = s.BlogName,
            Tagline = s.Tagline,
            TopBarText = s.TopBarText,
            BlogUrl = s.BlogUrl,
            NewsletterEnabled = s.NewsletterEnabled,
            CommentsEnabled = s.CommentsEnabled,
            MaintenanceMode = s.MaintenanceMode,
            TickerEnabled = s.TickerEnabled,
            FeaturedEnabled = s.FeaturedEnabled,
            Instagram = s.Instagram,
            YouTube = s.YouTube,
            Twitter = s.Twitter,
            LinkedIn = s.LinkedIn,
        };
        return View(vm);
    }

    // POST /admin/configuracoes/blog
    [HttpPost("configuracoes/blog")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBlogInfo(string blogName, string tagline, string topBarText, string blogUrl)
    {
        if (string.IsNullOrWhiteSpace(blogName))
        {
            TempData["Error"] = "O nome do blog é obrigatório.";
            return RedirectToAction(nameof(Settings));
        }

        var s = await _settings.GetAsync();
        s.BlogName = blogName;
        s.Tagline = tagline ?? string.Empty;
        s.TopBarText = topBarText ?? string.Empty;
        s.BlogUrl = blogUrl ?? string.Empty;

        await _settings.SaveAsync(s);

        TempData["Success"] = "Informações do blog atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    // POST /admin/configuracoes/funcionalidades
    [HttpPost("configuracoes/funcionalidades")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFeatureSettings(bool newsletterEnabled, bool commentsEnabled, bool maintenanceMode, bool tickerEnabled, bool featuredEnabled)
    {
        var s = await _settings.GetAsync();
        s.NewsletterEnabled = newsletterEnabled;
        s.CommentsEnabled = commentsEnabled;
        s.MaintenanceMode = maintenanceMode;
        s.TickerEnabled = tickerEnabled;
        s.FeaturedEnabled = featuredEnabled;

        await _settings.SaveAsync(s);

        TempData["Success"] = "Funcionalidades atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    // POST /admin/configuracoes/social
    [HttpPost("configuracoes/social")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSocialSettings(string? instagram, string? youtube, string? twitter, string? linkedIn)
    {
        var s = await _settings.GetAsync();
        s.Instagram = instagram;
        s.YouTube = youtube;
        s.Twitter = twitter;
        s.LinkedIn = linkedIn;

        await _settings.SaveAsync(s);

        TempData["Success"] = "Redes sociais atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    // ══════════════════════════════════════════════
    //  ALTERAR SENHA  POST /admin/configuracoes/senha
    // ══════════════════════════════════════════════
    [HttpPost("configuracoes/senha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Verifique os campos de senha.";
            return RedirectToAction(nameof(Settings));
        }

        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // Atualiza UserName se diferente
        if (user.UserName != vm.UserName)
        {
            user.UserName = vm.UserName;
            await _users.UpdateAsync(user);
        }

        // Troca senha somente se nova senha informada
        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var result = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Settings));
            }
        }

        TempData["Success"] = "Credenciais atualizadas!";
        return RedirectToAction(nameof(Settings));
    }
}