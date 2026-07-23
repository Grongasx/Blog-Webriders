// ============================================================================
// 1. NAMESPACES E DEPENDÊNCIAS
// ============================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Painel de administração central do ThrottleBlog.
/// Todas as rotas deste controlador exigem privilégios de nível 'Admin'.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    // ============================================================================
    // 2. PROPRIEDADES PRIVADAS (INJEÇÃO DE DEPENDÊNCIA)
    // ============================================================================
    private readonly IPostService                _posts;
    private readonly ICategoryService            _categories;
    private readonly ISettingsService            _settings;
    private readonly IDashboardService           _dashboard;
    private readonly IImageUploadService         _uploads;
    private readonly INewsletterService          _newsletter;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IMemoryCache                _cache;

    // ============================================================================
    // 3. CONSTRUTOR
    // ============================================================================
    public AdminController(
        IPostService                posts,
        ICategoryService            categories,
        ISettingsService            settings,
        IDashboardService           dashboard,
        IImageUploadService         uploads,
        INewsletterService          newsletter,
        UserManager<ApplicationUser> users,
        IMemoryCache                cache)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
        _dashboard  = dashboard;
        _uploads    = uploads;
        _newsletter = newsletter;
        _users      = users;
        _cache      = cache;
    }

    // Chave de cache idêntica à usada no PostController
    private static string PostCacheKey(string slug) => $"post_html_{slug}";

    #region DASHBOARD PRINCIPAL

    /// <summary>
    /// GET /admin | GET /admin/dashboard
    /// Carrega as métricas consolidadas, contadores e gráficos do painel geral.
    /// </summary>
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        var vm = await _dashboard.GetDashboardAsync();
        return View(vm);
    }

    #endregion

    #region GESTÃO DE ARTIGOS (CMS)

    /// <summary>
    /// GET /admin/posts
    /// Lista os posts no painel com filtros por busca textual, categoria, status e paginação.
    /// </summary>
    [HttpGet("posts")]
    public async Task<IActionResult> Posts(string? q, string? category, string? status, int page = 1)
    {
        const int pageSize = 8;
        var total = await _posts.AdminCountAsync(q, category, status);
        var list  = await _posts.AdminListAsync(page, pageSize, q, category, status);

        var vm = new PostListViewModel
        {
            Posts          = list,
            Categories     = await _categories.GetAllAsync(),
            SearchQuery    = q,
            CategoryFilter = category,
            StatusFilter   = status,
            Pagination     = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };
        return View(vm);
    }

    /// <summary>
    /// GET /admin/posts/novo
    /// Abre o formulário de composição para um novo artigo em estado de rascunho.
    /// </summary>
    [HttpGet("posts/novo")]
    public async Task<IActionResult> NewPost()
    {
        var vm = new PostFormViewModel
        {
            Status     = PostStatus.Draft,
            Categories = await _categories.GetAllAsync()
        };
        return View("PostForm", vm);
    }

    /// <summary>
    /// GET /admin/posts/{id}/editar
    /// Carrega os dados de um post existente para edição.
    /// </summary>
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
            GalleryImages = post.GalleryImages,
            ReadingTime   = post.ReadingTime,
            Status        = post.Status,
            IsFeatured    = post.IsFeatured,
            CategoryId    = post.CategoryId,
            Categories    = await _categories.GetAllAsync()
        };
        return View("PostForm", vm);
    }

    /// <summary>
    /// POST /admin/posts/salvar
    /// Processa a persistência de posts novos (Create) ou atualizações (Update).
    /// Invalida o cache de HTML renderizado para garantir que as mudanças
    /// apareçam imediatamente sem necessidade de reiniciar a aplicação.
    /// </summary>
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
            // Busca o slug ANTES de atualizar (pode ter mudado se o título mudou)
            var existing = await _posts.GetByIdAsync(vm.Id);
            if (existing is not null)
                _cache.Remove(PostCacheKey(existing.Slug));

            await _posts.UpdateAsync(vm);

            // Remove também pelo slug novo (caso UpdateAsync gere um slug diferente)
            var updated = await _posts.GetByIdAsync(vm.Id);
            if (updated is not null)
                _cache.Remove(PostCacheKey(updated.Slug));

            TempData["Success"] = "Post atualizado!";
        }
        return RedirectToAction(nameof(Posts));
    }

    /// <summary>
    /// POST /admin/posts/{id}/publicar
    /// Altera o status do post diretamente para publicado (exige imagem de destaque).
    /// </summary>
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
        
        _cache.Remove(PostCacheKey(post.Slug));
        await _posts.PublishAsync(id);
        TempData["Success"] = "Post publicado!";
        return RedirectToAction(nameof(Posts));
    }

    /// <summary>
    /// POST /admin/posts/{id}/despublicar
    /// Reverte o status de um post publicado de volta para rascunho (Draft).
    /// </summary>
    [HttpPost("posts/{id:int}/despublicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        var post = await _posts.GetByIdAsync(id);
        if (post is not null)
            _cache.Remove(PostCacheKey(post.Slug));

        await _posts.UnpublishAsync(id);
        TempData["Success"] = "Post movido para rascunho.";
        return RedirectToAction(nameof(Posts));
    }

    /// <summary>
    /// POST /admin/posts/{id}/excluir
    /// Remove permanentemente um post do banco de dados.
    /// </summary>
    [HttpPost("posts/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _posts.GetByIdAsync(id);
        if (post is not null)
            _cache.Remove(PostCacheKey(post.Slug));

        await _posts.DeleteAsync(id);
        TempData["Success"] = "Post excluído.";
        return RedirectToAction(nameof(Posts));
    }

    #endregion

    #region MÍDIA E CAPTURA DE DADOS (SCRAPER)

    /// <summary>
    /// POST /admin/media/upload
    /// Recebe arquivos de imagem enviados via editor ou upload direto e retorna a URL pública correspondente.
    /// </summary>
    [HttpPost("media/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado." });

        try
        {
            var userId = _users.GetUserId(User)!;
            var url    = await _uploads.UploadAsync(file, userId);
            return Json(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /admin/scrape-product?url=...
    /// Varre e minera tags OpenGraph de e-commerces da Webriders para gerar cartões comerciais automáticos nos artigos.
    /// </summary>
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

            var variations     = new List<string>();
            var variantMatches = System.Text.RegularExpressions.Regex.Matches(html, @"<option[^>]*>([^<]+)</option>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            foreach (System.Text.RegularExpressions.Match match in variantMatches)
            {
                var txt = match.Groups[1].Value.Trim();
                if (!txt.Contains("Selecione") && !txt.Contains("escolha") && txt.Length < 30 && variations.Count < 5)
                    variations.Add(txt);
            }

            if (!variations.Any())
                variations.Add("Tamanho Único");

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

    #endregion

    #region GESTÃO DE CATEGORIAS

    /// <summary>
    /// GET /admin/categorias
    /// Lista todas as categorias cadastradas, calculando dinamicamente a volumetria de artigos vinculados.
    /// </summary>
    [HttpGet("categorias")]
    public async Task<IActionResult> Categories()
    {
        var cats     = await _categories.GetAllAsync();
        var allPosts = await _posts.AdminListAsync(1, int.MaxValue, null, null, null);

        var vm = cats.Select(c => new CategoryFormViewModel
        {
            Id        = c.Id,
            Name      = c.Name,
            ImageUrl  = c.ImageUrl,
            SortOrder = c.SortOrder,
            PostCount = allPosts.Count(p => p.CategoryId == c.Id)
        }).ToList();

        return View(vm);
    }

    /// <summary>
    /// GET /admin/categorias/nova
    /// Renderiza o formulário limpo para a criação de categorias.
    /// </summary>
    [HttpGet("categorias/nova")]
    public IActionResult NewCategory() => View("CategoryForm", new CategoryFormViewModel());

    /// <summary>
    /// GET /admin/categorias/{id}/editar
    /// Recupera os dados estruturais de uma categoria para modificações.
    /// </summary>
    [HttpGet("categorias/{id:int}/editar")]
    public async Task<IActionResult> EditCategory(int id)
    {
        var cat = await _categories.GetByIdAsync(id);
        if (cat is null) return NotFound();
        
        return View("CategoryForm", new CategoryFormViewModel
        {
            Id        = cat.Id,
            Name      = cat.Name,
            ImageUrl  = cat.ImageUrl,
            SortOrder = cat.SortOrder
        });
    }

    /// <summary>
    /// POST /admin/categorias/salvar
    /// Processa a inserção de novas categorias ou atualiza ordenações/metadados de existentes.
    /// </summary>
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

    /// <summary>
    /// POST /admin/categorias/{id}/excluir
    /// Remove uma categoria do sistema. Dependendo das FKs (OnDelete.Restrict), requer esvaziamento prévio de posts.
    /// </summary>
    [HttpPost("categorias/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categories.DeleteAsync(id);
        TempData["Success"] = "Categoria removida.";
        return RedirectToAction(nameof(Categories));
    }

    #endregion

    #region GESTÃO DE ASSINANTES (NEWSLETTER)

    /// <summary>
    /// GET /admin/newsletter
    /// Lista os leads inscritos na base de marketing com paginação e filtros de categoria (Tier) e status (Ativo/Inativo).
    /// </summary>
    [HttpGet("newsletter")]
    public async Task<IActionResult> NewsletterSubscribers(string? q, string? tier, string? status, int page = 1)
    {
        const int pageSize = 15;
        var (items, total) = await _newsletter.ListAsync(page, pageSize, q, tier, status);
        var counts         = await _newsletter.CountByTierAsync();

        var vm = new AdminNewsletterListViewModel
        {
            Subscribers  = items,
            SearchQuery  = q,
            TierFilter   = tier,
            StatusFilter = status,
            CommonTotal  = counts.Common,
            PremiumTotal = counts.Premium,
            Pagination   = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = (int)Math.Ceiling((double)total / pageSize),
                TotalItems  = total,
                PageSize    = pageSize
            }
        };
        return View("Newsletter", vm);
    }

    /// <summary>
    /// POST /admin/newsletter/{id}/desativar
    /// Soft-delete/Desativação de leads na listagem de marketing do blog.
    /// </summary>
    [HttpPost("newsletter/{id:int}/desativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateSubscriber(int id)
    {
        await _newsletter.SetActiveAsync(id, false);
        TempData["Success"] = "Assinante desativado.";
        return RedirectToAction(nameof(NewsletterSubscribers));
    }

    /// <summary>
    /// POST /admin/newsletter/{id}/reativar
    /// Reativa a assinatura de um lead inativo para restabelecer recebimento de campanhas.
    /// </summary>
    [HttpPost("newsletter/{id:int}/reativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateSubscriber(int id)
    {
        await _newsletter.SetActiveAsync(id, true);
        TempData["Success"] = "Assinante reativado.";
        return RedirectToAction(nameof(NewsletterSubscribers));
    }

    #endregion

    #region CONFIGURAÇÕES GLOBAIS E SEGURANÇA

    /// <summary>
    /// GET /admin/configuracoes
    /// Consolida as configurações globais de identidade visual, chaves de recursos e redes sociais.
    /// </summary>
    [HttpGet("configuracoes")]
    public async Task<IActionResult> Settings()
    {
        var s = await _settings.GetAsync();
        var vm = new SettingsViewModel
        {
            BlogName          = s.BlogName,
            Tagline           = s.Tagline,
            TopBarText        = s.TopBarText,
            BlogUrl           = s.BlogUrl,
            NewsletterEnabled = s.NewsletterEnabled,
            CommentsEnabled   = s.CommentsEnabled,
            MaintenanceMode   = s.MaintenanceMode,
            TickerEnabled     = s.TickerEnabled,
            FeaturedEnabled   = s.FeaturedEnabled,
            Instagram         = s.Instagram,
            YouTube           = s.YouTube,
            Twitter           = s.Twitter,
            LinkedIn          = s.LinkedIn,
        };
        return View(vm);
    }

    /// <summary>
    /// POST /admin/configuracoes/blog
    /// Atualiza os textos institucionais básicos e metadados estruturais do blog.
    /// </summary>
    [HttpPost("configuracoes/blog")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBlogInfo(string blogName, string tagline, string topBarText, string blogUrl)
    {
        if (string.IsNullOrWhiteSpace(blogName))
        {
            TempData["Error"] = "O nome do blog é obrigatório.";
            return RedirectToAction(nameof(Settings));
        }

        var s        = await _settings.GetAsync();
        s.BlogName   = blogName;
        s.Tagline    = tagline ?? string.Empty;
        s.TopBarText = topBarText ?? string.Empty;
        s.BlogUrl    = blogUrl ?? string.Empty;

        await _settings.SaveAsync(s);
        TempData["Success"] = "Informações do blog atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    /// <summary>
    /// POST /admin/configuracoes/funcionalidades
    /// Habilita ou desabilita flags globais do ecossistema.
    /// </summary>
    [HttpPost("configuracoes/funcionalidades")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFeatureSettings(bool newsletterEnabled, bool commentsEnabled, bool maintenanceMode, bool tickerEnabled, bool featuredEnabled)
    {
        var s               = await _settings.GetAsync();
        s.NewsletterEnabled = newsletterEnabled;
        s.CommentsEnabled   = commentsEnabled;
        s.MaintenanceMode   = maintenanceMode;
        s.TickerEnabled     = tickerEnabled;
        s.FeaturedEnabled   = featuredEnabled;

        await _settings.SaveAsync(s);
        TempData["Success"] = "Funcionalidades atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    /// <summary>
    /// POST /admin/configuracoes/social
    /// Atualiza os links sociais indexados nos rodapés e cabeçalhos públicos do portal.
    /// </summary>
    [HttpPost("configuracoes/social")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSocialSettings(string? instagram, string? youtube, string? twitter, string? linkedIn)
    {
        var s       = await _settings.GetAsync();
        s.Instagram = instagram;
        s.YouTube   = youtube;
        s.Twitter   = twitter;
        s.LinkedIn  = linkedIn;

        await _settings.SaveAsync(s);
        TempData["Success"] = "Redes sociais atualizadas!";
        return RedirectToAction(nameof(Settings));
    }

    /// <summary>
    /// POST /admin/configuracoes/senha
    /// Atualiza as credenciais administrativas locais via Token Identity.
    /// </summary>
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

        if (user.UserName != vm.UserName)
        {
            user.UserName = vm.UserName;
            await _users.UpdateAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token  = await _users.GeneratePasswordResetTokenAsync(user);
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

    #endregion
}