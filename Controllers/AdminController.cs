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
    private readonly IPostService       _posts;
    private readonly ICategoryService   _categories;
    private readonly ISettingsService   _settings;
    private readonly IDashboardService  _dashboard;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(
        IPostService      posts,
        ICategoryService  categories,
        ISettingsService  settings,
        IDashboardService dashboard,
        UserManager<ApplicationUser> users)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
        _dashboard  = dashboard;
        _users      = users;
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

    // ══════════════════════════════════════════════
    //  NOVO POST  GET /admin/posts/novo
    // ══════════════════════════════════════════════
    [HttpGet("posts/novo")]
    public async Task<IActionResult> NewPost()
    {
        var vm = new PostFormViewModel
        {
            Categories = await _categories.GetAllAsync()
        };
        return View("PostForm", vm);
    }

    // ══════════════════════════════════════════════
    //  EDITAR POST  GET /admin/posts/{id}/editar
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
            TagsRaw       = string.Join(", ", post.PostTags.Select(pt => pt.Tag.Name)),
            Categories    = await _categories.GetAllAsync()
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
        await _posts.PublishAsync(id);
        TempData["Success"] = "Post publicado!";
        return RedirectToAction(nameof(Posts));
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
            Id       = cat.Id, Name     = cat.Name,
            ImageUrl = cat.ImageUrl, SortOrder = cat.SortOrder
        });
    }

    // POST /admin/categorias/salvar
    [HttpPost("categorias/salvar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("CategoryForm", vm);

        if (vm.Id == 0) await _categories.CreateAsync(vm);
        else             await _categories.UpdateAsync(vm);

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
    //  CONFIGURAÇÕES  GET /admin/configuracoes
    // ══════════════════════════════════════════════
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

    // POST /admin/configuracoes
    [HttpPost("configuracoes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SettingsViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _settings.SaveFromViewModelAsync(vm);
        TempData["Success"] = "Configurações salvas!";
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
}
