using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Data;
using ThrottleBlog.Models;
using ThrottleBlog.ViewModels;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers; 
using System.Text;            
using System.Text.Json;

namespace ThrottleBlog.Services;

// ── SLUG HELPER ──────────────────────────────────────────────────
public static class SlugHelper
{
    public static string Generate(string text)
    {
        var slug = text.ToLowerInvariant()
            .Replace("á","a").Replace("à","a").Replace("ã","a").Replace("â","a")
            .Replace("é","e").Replace("ê","e").Replace("í","i")
            .Replace("ó","o").Replace("õ","o").Replace("ô","o")
            .Replace("ú","u").Replace("ü","u").Replace("ç","c")
            .Replace(" ", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-");
        return slug.Trim('-');
    }
}

// ════════════════════════════════════════════════════════════════
//  POST SERVICE
// ════════════════════════════════════════════════════════════════
public interface IPostService
{
    Task<List<Post>> GetPublishedAsync(int page, int pageSize, string? category = null, string? search = null);
    Task<int>        CountPublishedAsync(string? category = null, string? search = null);
    Task<Post?>      GetBySlugAsync(string slug);
    Task<Post?>      GetByIdAsync(int id);
    Task<List<Post>> GetFeaturedAsync(int count = 3);
    Task<List<Post>> GetLatestAsync(int count = 6);
    Task<List<Post>> GetRelatedAsync(int postId, int categoryId, int count = 3);
    Task<List<Post>> GetMostReadAsync(int count = 5);
    Task IncrementViewAsync(string slug);
    Task<IReadOnlyList<Post>> GetFeaturedByCategoryAsync(string categorySlug, int count);

    // admin
    Task<List<Post>> AdminListAsync(int page, int pageSize, string? search, string? category, string? status);
    Task<int>        AdminCountAsync(string? search, string? category, string? status);
    Task<Post>       CreateAsync(PostFormViewModel vm, string authorId);
    Task<Post>       UpdateAsync(PostFormViewModel vm);
    Task             DeleteAsync(int id);
    Task             PublishAsync(int id);
    Task             UnpublishAsync(int id);
}

public class PostService : IPostService
{
    private readonly AppDbContext _db;
    public PostService(AppDbContext db) => _db = db;

    public async Task<List<Post>> GetPublishedAsync(int page, int pageSize, string? category = null, string? search = null)
        => await BasePublicQuery(category, search)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<int> CountPublishedAsync(string? category = null, string? search = null)
        => await BasePublicQuery(category, search).CountAsync();

    public async Task<Post?> GetBySlugAsync(string slug)
        => await _db.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Comments.Where(c => c.Status == CommentStatus.Approved))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PostStatus.Published);

    public async Task<Post?> GetByIdAsync(int id)
        => await _db.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Post>> GetFeaturedAsync(int count = 3)
        => await _db.Posts
            .Include(p => p.Category).Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Published && p.IsFeatured)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<List<Post>> GetLatestAsync(int count = 6)
        => await _db.Posts
            .Include(p => p.Category).Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<List<Post>> GetRelatedAsync(int postId, int categoryId, int count = 3)
        => await _db.Posts
            .Include(p => p.Category).Include(p => p.Author)
            .Where(p => p.CategoryId == categoryId && p.Id != postId && p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<List<Post>> GetMostReadAsync(int count = 5)
        => await _db.Posts
            .Include(p => p.Category).Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.ViewCount)      // ← usa ViewCount real
            .Take(count).ToListAsync();
    
    public async Task<IReadOnlyList<Post>> GetFeaturedByCategoryAsync(
    string categorySlug, int count)
{
    return await _db.Posts
        .Include(p => p.Category)
        .Include(p => p.Author)
        .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
        .Where(p => p.Status == PostStatus.Published
                 && p.IsFeatured
                 && p.Category.Slug == categorySlug)
        .OrderByDescending(p => p.PublishedAt)
        .Take(count)
        .ToListAsync();
}

    // ── Incrementa ViewCount atomicamente ───────────────────────
    public async Task IncrementViewAsync(string slug)
    {
        await _db.Posts
            .Where(p => p.Slug == slug)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
    }

    public async Task<List<Post>> AdminListAsync(int page, int pageSize, string? search, string? category, string? status)
        => await BaseAdminQuery(search, category, status)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<int> AdminCountAsync(string? search, string? category, string? status)
        => await BaseAdminQuery(search, category, status).CountAsync();

    public async Task<Post> CreateAsync(PostFormViewModel vm, string authorId)
    {
        var slug = await EnsureUniqueSlugAsync(SlugHelper.Generate(vm.Title));
        var post = new Post
        {
            Title         = vm.Title, Slug          = slug,
            Excerpt       = vm.Excerpt, Content     = vm.Content,
            FeaturedImage = vm.FeaturedImage, ReadingTime = vm.ReadingTime,
            Status        = vm.Status, IsFeatured   = vm.IsFeatured,
            CategoryId    = vm.CategoryId, AuthorId = authorId,
            CreatedAt     = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            PublishedAt   = vm.Status == PostStatus.Published ? DateTime.UtcNow : null
        };
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        await SyncTagsAsync(post.Id, vm.TagsRaw);
        return post;
    }

    public async Task<Post> UpdateAsync(PostFormViewModel vm)
    {
        var post = await _db.Posts.FindAsync(vm.Id) ?? throw new KeyNotFoundException();
        var oldStatus = post.Status;
        post.Title         = vm.Title; post.Excerpt = vm.Excerpt;
        post.Content       = vm.Content; post.FeaturedImage = vm.FeaturedImage;
        post.ReadingTime   = vm.ReadingTime; post.Status = vm.Status;
        post.IsFeatured    = vm.IsFeatured; post.CategoryId = vm.CategoryId;
        post.UpdatedAt     = DateTime.UtcNow;
        if (vm.Status == PostStatus.Published && oldStatus == PostStatus.Draft)
            post.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await SyncTagsAsync(post.Id, vm.TagsRaw);
        return post;
    }

    public async Task DeleteAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is not null) { _db.Posts.Remove(post); await _db.SaveChangesAsync(); }
    }

    public async Task PublishAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id); if (post is null) return;
        post.Status = PostStatus.Published; post.PublishedAt ??= DateTime.UtcNow; post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UnpublishAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id); if (post is null) return;
        post.Status = PostStatus.Draft; post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private IQueryable<Post> BasePublicQuery(string? category, string? search)
    {
        var q = _db.Posts.Include(p => p.Category).Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Where(p => p.Status == PostStatus.Published).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(p =>
                EF.Functions.ILike(p.Title, term) ||
                EF.Functions.ILike(p.Excerpt, term) ||
                EF.Functions.ILike(p.Content, term) ||
                p.PostTags.Any(pt => EF.Functions.ILike(pt.Tag.Name, term)));
        }
        return q;
    }

    private IQueryable<Post> BaseAdminQuery(string? search, string? category, string? status)
    {
        var q = _db.Posts.Include(p => p.Category).Include(p => p.Author).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))   q = q.Where(p => p.Title.Contains(search) || p.Author.DisplayName.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category.Name == category);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PostStatus>(status, out var ps))
            q = q.Where(p => p.Status == ps);
        return q;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug; var i = 1;
        while (await _db.Posts.AnyAsync(p => p.Slug == slug)) slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private async Task SyncTagsAsync(int postId, string tagsRaw)
    {
        _db.PostTags.RemoveRange(_db.PostTags.Where(pt => pt.PostId == postId));
        if (string.IsNullOrWhiteSpace(tagsRaw)) { await _db.SaveChangesAsync(); return; }
        var names = tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim()).Where(t => t.Length > 0).Distinct();
        foreach (var name in names)
        {
            var slug = SlugHelper.Generate(name);
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == slug) ?? new Tag { Name = name, Slug = slug };
            if (tag.Id == 0) _db.Tags.Add(tag);
            await _db.SaveChangesAsync();
            _db.PostTags.Add(new PostTag { PostId = postId, TagId = tag.Id });
        }
        await _db.SaveChangesAsync();
    }
}

// ════════════════════════════════════════════════════════════════
//  CATEGORY SERVICE
// ════════════════════════════════════════════════════════════════
public interface ICategoryService
{
    Task<List<Category>>      GetAllAsync();
    Task<Category?>           GetByIdAsync(int id);
    Task<Category?>           GetBySlugAsync(string slug);
    Task<Category>            CreateAsync(CategoryFormViewModel vm);
    Task<Category>            UpdateAsync(CategoryFormViewModel vm);
    Task                      DeleteAsync(int id);
    Task<List<CategoryCount>> GetBreakdownAsync();
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    public CategoryService(AppDbContext db) => _db = db;
    public Task<List<Category>> GetAllAsync() => _db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
    public Task<Category?> GetByIdAsync(int id) => _db.Categories.FindAsync(id).AsTask()!;
    public Task<Category?> GetBySlugAsync(string slug) => _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
    public async Task<Category> CreateAsync(CategoryFormViewModel vm)
    {
        var cat = new Category { Name = vm.Name, Slug = SlugHelper.Generate(vm.Name), ImageUrl = vm.ImageUrl, SortOrder = vm.SortOrder };
        _db.Categories.Add(cat); await _db.SaveChangesAsync(); return cat;
    }
    public async Task<Category> UpdateAsync(CategoryFormViewModel vm)
    {
        var cat = await _db.Categories.FindAsync(vm.Id) ?? throw new KeyNotFoundException();
        cat.Name = vm.Name; cat.Slug = SlugHelper.Generate(vm.Name); cat.ImageUrl = vm.ImageUrl; cat.SortOrder = vm.SortOrder;
        await _db.SaveChangesAsync(); return cat;
    }
    public async Task DeleteAsync(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is not null) { _db.Categories.Remove(cat); await _db.SaveChangesAsync(); }
    }
    public async Task<List<CategoryCount>> GetBreakdownAsync()
        => await _db.Categories
            .Select(c => new CategoryCount(c.Name, c.Posts.Count(p => p.Status == PostStatus.Published)))
            .ToListAsync();
}

// ════════════════════════════════════════════════════════════════
//  COMMENT SERVICE
// ════════════════════════════════════════════════════════════════
public interface ICommentService
{
    Task<Comment> AddAsync(int postId, string name, string email, string body, string? userId = null);
    Task ApproveAsync(int id);
    Task SpamAsync(int id);
    Task DeleteAsync(int id);
    Task<List<Comment>> GetAdminListAsync(string? status = null);
    Task<int>           PendingCountAsync();
}

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly ISettingsService _settings;
    public CommentService(AppDbContext db, ISettingsService settings) { _db = db; _settings = settings; }

    public async Task<Comment> AddAsync(int postId, string name, string email, string body, string? userId = null)
    {
        var s = await _settings.GetAsync();
        // Auto-approve se CommentsEnabled, senão fica Pending
        var status = s.CommentsEnabled ? CommentStatus.Approved : CommentStatus.Pending;
        var comment = new Comment
        {
            PostId = postId, AuthorName = name, AuthorEmail = email,
            Body = body, Status = status, UserId = userId
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    public async Task ApproveAsync(int id)
    {
        var c = await _db.Comments.FindAsync(id); if (c is null) return;
        c.Status = CommentStatus.Approved; await _db.SaveChangesAsync();
    }

    public async Task SpamAsync(int id)
    {
        var c = await _db.Comments.FindAsync(id); if (c is null) return;
        c.Status = CommentStatus.Spam; await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var c = await _db.Comments.FindAsync(id);
        if (c is not null) { _db.Comments.Remove(c); await _db.SaveChangesAsync(); }
    }

    public async Task<List<Comment>> GetAdminListAsync(string? status = null)
    {
        var q = _db.Comments.Include(c => c.Post).OrderByDescending(c => c.CreatedAt).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CommentStatus>(status, out var cs))
            q = q.Where(c => c.Status == cs);
        return await q.Take(100).ToListAsync();
    }

    public Task<int> PendingCountAsync()
        => _db.Comments.CountAsync(c => c.Status == CommentStatus.Pending);
}

// ════════════════════════════════════════════════════════════════
//  SETTINGS SERVICE
// ════════════════════════════════════════════════════════════════
public interface ISettingsService
{
    Task<BlogSettings> GetAsync();
    Task               SaveAsync(BlogSettings settings);
    Task               SaveFromViewModelAsync(SettingsViewModel vm);
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    public SettingsService(AppDbContext db) => _db = db;
    public async Task<BlogSettings> GetAsync() => await _db.BlogSettings.FindAsync(1) ?? new BlogSettings();
    public async Task SaveAsync(BlogSettings settings) { _db.BlogSettings.Update(settings); await _db.SaveChangesAsync(); }
    public async Task SaveFromViewModelAsync(SettingsViewModel vm)
    {
        // 1. Busca a configuração atual do banco
        var s = await GetAsync();

        // 2. Mapeia todos os campos manualmente
        s.BlogName = vm.BlogName;
        s.Tagline = vm.Tagline;
        s.TopBarText = vm.TopBarText;
        s.BlogUrl = vm.BlogUrl;
        
        s.NewsletterEnabled = vm.NewsletterEnabled;
        s.CommentsEnabled = vm.CommentsEnabled;
        s.MaintenanceMode = vm.MaintenanceMode; // Agora o valor real chega aqui
        s.TickerEnabled = vm.TickerEnabled;
        s.FeaturedEnabled = vm.FeaturedEnabled;
        
        s.Instagram = vm.Instagram;
        s.YouTube = vm.YouTube;
        s.Twitter = vm.Twitter;
        s.LinkedIn = vm.LinkedIn;

        // 3. Salva no banco
        await SaveAsync(s);
    }
}
// ════════════════════════════════════════════════════════════════
//  Maintenances Services
// ════════════════════════════════════════════════════════════════
public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ISettingsService settings)
    {
        var path = context.Request.Path.Value?.ToLower();
        
        // Ignora o admin
        if (path != null && path.StartsWith("/admin"))
        {
            await _next(context);
            return;
        }

        // Busca o dado
        var s = await settings.GetAsync();

        // --- DEBUG: ADICIONE ISSO ---
        Console.WriteLine($"--- DEBUG: O sistema leu MaintenanceMode como: {s?.MaintenanceMode} ---");
        // ----------------------------

        if (s != null && s.MaintenanceMode)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Estamos em manutenção. Voltaremos em breve!");
            return;
        }

        await _next(context);
    }
}

// ════════════════════════════════════════════════════════════════
//  DASHBOARD SERVICE
// ════════════════════════════════════════════════════════════════
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext     _db;
    private readonly ICategoryService _cats;
    private readonly ICommentService  _comments;
    public DashboardService(AppDbContext db, ICategoryService cats, ICommentService comments)
    { _db = db; _cats = cats; _comments = comments; }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var today   = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-6);
        var all     = await _db.Posts.Include(p => p.Category).Include(p => p.Author).ToListAsync();

        var weekly = Enumerable.Range(0, 7).Select(i => {
            var day   = weekAgo.AddDays(i);
            var label = day.ToString("ddd", new System.Globalization.CultureInfo("pt-BR")).ToUpper()[..3];
            return new DailyActivity(label, all.Count(p => p.CreatedAt.Date == day));
        }).ToList();

        return new DashboardViewModel
        {
            TotalPosts        = all.Count,
            PublishedPosts    = all.Count(p => p.Status == PostStatus.Published),
            DraftPosts        = all.Count(p => p.Status == PostStatus.Draft),
            TotalCategories   = await _db.Categories.CountAsync(),
            PendingComments   = await _comments.PendingCountAsync(),
            RecentPosts       = all.OrderByDescending(p => p.CreatedAt).Take(4).ToList(),
            CategoryBreakdown = await _cats.GetBreakdownAsync(),
            WeeklyActivity    = weekly
        };
    }
}

// ════════════════════════════════════════════════════════════════
//  IMAGE UPLOAD SERVICE
// ════════════════════════════════════════════════════════════════
public interface IImageUploadService
{
    Task<string> UploadAsync(IFormFile file, string uploadedBy);
    Task<List<UploadedFile>> GetAllAsync();
    Task DeleteAsync(int id);
}

public class LocalImageUploadService : IImageUploadService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    // Tipos e tamanhos permitidos
    private static readonly string[] AllowedTypes  = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    public LocalImageUploadService(AppDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    public async Task<string> UploadAsync(IFormFile file, string uploadedBy)
    {
        if (file.Length > MaxSizeBytes)
            throw new InvalidOperationException("Arquivo muito grande. Máximo: 5 MB.");
        if (!AllowedTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Tipo de arquivo não permitido.");

        // Cria diretório /wwwroot/uploads/{ano}/{mes}
        var folder = Path.Combine(_env.WebRootPath, "uploads",
                     DateTime.UtcNow.Year.ToString(), DateTime.UtcNow.Month.ToString("D2"));
        Directory.CreateDirectory(folder);

        // Nome único para evitar colisões
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, safeName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        // URL relativa acessível pelo browser
        var url = $"/uploads/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{safeName}";

        _db.UploadedFiles.Add(new UploadedFile
        {
            FileName   = file.FileName,
            Url        = url,
            SizeBytes  = file.Length,
            MimeType   = file.ContentType,
            UploadedBy = uploadedBy
        });
        await _db.SaveChangesAsync();
        return url;
    }

    public Task<List<UploadedFile>> GetAllAsync()
        => _db.UploadedFiles.OrderByDescending(f => f.UploadedAt).ToListAsync();

    public async Task DeleteAsync(int id)
    {
        var f = await _db.UploadedFiles.FindAsync(id);
        if (f is null) return;
        var physPath = Path.Combine(_env.WebRootPath, f.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physPath)) File.Delete(physPath);
        _db.UploadedFiles.Remove(f);
        await _db.SaveChangesAsync();
    }
}

// ════════════════════════════════════════════════════════════════
//  NEWSLETTER SERVICE
// ════════════════════════════════════════════════════════════════
public enum NewsletterSubscribeOutcome
{
    Success,
    AlreadySubscribed,
    UpgradedToPremium,
    Reactivated
}

public class NewsletterSubscribeResult
{
    public NewsletterSubscribeOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Success => Outcome is NewsletterSubscribeOutcome.Success
        or NewsletterSubscribeOutcome.UpgradedToPremium
        or NewsletterSubscribeOutcome.Reactivated;
}

public interface INewsletterService
{
    Task<NewsletterSubscribeResult> SubscribeAsync(string name, string email, NewsletterTier tier, string? phone = null);
    Task<(List<NewsletterSubscriber> Items, int Total)> ListAsync(int page, int pageSize, string? search, string? tier, string? status);
    Task<(int Common, int Premium)> CountByTierAsync(bool activeOnly = true);
    Task<bool> SetActiveAsync(int id, bool isActive);
}

public class NewsletterService : INewsletterService
{
    private readonly AppDbContext _db;
    public NewsletterService(AppDbContext db) => _db = db;

    public async Task<NewsletterSubscribeResult> SubscribeAsync(string name, string email, NewsletterTier tier, string? phone = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == normalized);

        if (tier == NewsletterTier.Premium && string.IsNullOrWhiteSpace(phone))
        {
            return new NewsletterSubscribeResult
            {
                Outcome = NewsletterSubscribeOutcome.AlreadySubscribed,
                Message = "Telefone é obrigatório para assinatura Premium."
            };
        }

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                ApplySubscription(existing, name, tier, phone);
                await _db.SaveChangesAsync();
                return new NewsletterSubscribeResult
                {
                    Outcome = NewsletterSubscribeOutcome.Reactivated,
                    Message = tier == NewsletterTier.Premium
                        ? "Bem-vindo de volta ao Webriders Premium!"
                        : "Inscrição reativada! Bem-vindo ao Webriders."
                };
            }

            if (tier == NewsletterTier.Premium && existing.Tier == NewsletterTier.Common)
            {
                ApplySubscription(existing, name, NewsletterTier.Premium, phone);
                await _db.SaveChangesAsync();
                return new NewsletterSubscribeResult
                {
                    Outcome = NewsletterSubscribeOutcome.UpgradedToPremium,
                    Message = "Upgrade para Premium realizado! Você receberá conteúdos exclusivos."
                };
            }

            if (existing.Tier == NewsletterTier.Premium && tier == NewsletterTier.Common)
            {
                return new NewsletterSubscribeResult
                {
                    Outcome = NewsletterSubscribeOutcome.AlreadySubscribed,
                    Message = "Este e-mail já possui assinatura Premium."
                };
            }

            return new NewsletterSubscribeResult
            {
                Outcome = NewsletterSubscribeOutcome.AlreadySubscribed,
                Message = "Este e-mail já está cadastrado."
            };
        }

        _db.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Name  = name.Trim(),
            Email = normalized,
            Tier  = tier,
            Phone = tier == NewsletterTier.Premium ? phone?.Trim() : null
        });
        await _db.SaveChangesAsync();

        return new NewsletterSubscribeResult
        {
            Outcome = NewsletterSubscribeOutcome.Success,
            Message = tier == NewsletterTier.Premium
                ? "Assinatura Premium confirmada! Em breve você receberá novidades exclusivas."
                : "Inscrição realizada! Bem-vindo ao Webriders."
        };
    }

    public async Task<(List<NewsletterSubscriber> Items, int Total)> ListAsync(
        int page, int pageSize, string? search, string? tier, string? status)
    {
        var q = _db.NewsletterSubscribers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(s =>
                EF.Functions.ILike(s.Name, term) ||
                EF.Functions.ILike(s.Email, term) ||
                (s.Phone != null && EF.Functions.ILike(s.Phone, term)));
        }

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<NewsletterTier>(tier, true, out var t))
            q = q.Where(s => s.Tier == t);

        if (status == "active")   q = q.Where(s => s.IsActive);
        if (status == "inactive") q = q.Where(s => !s.IsActive);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(s => s.SubscribedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(int Common, int Premium)> CountByTierAsync(bool activeOnly = true)
    {
        var q = _db.NewsletterSubscribers.AsQueryable();
        if (activeOnly) q = q.Where(s => s.IsActive);
        var common  = await q.CountAsync(s => s.Tier == NewsletterTier.Common);
        var premium = await q.CountAsync(s => s.Tier == NewsletterTier.Premium);
        return (common, premium);
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive)
    {
        var sub = await _db.NewsletterSubscribers.FindAsync(id);
        if (sub is null) return false;
        sub.IsActive = isActive;
        await _db.SaveChangesAsync();
        return true;
    }

    private static void ApplySubscription(NewsletterSubscriber sub, string name, NewsletterTier tier, string? phone)
    {
        sub.Name         = name.Trim();
        sub.Tier         = tier;
        sub.Phone        = tier == NewsletterTier.Premium ? phone?.Trim() : sub.Phone;
        sub.IsActive     = true;
        sub.SubscribedAt = DateTime.UtcNow;
    }
}

// ══════════════════════════════════════════════════════════════
//  Configuração — lida do appsettings.json
// ══════════════════════════════════════════════════════════════
public class ResendOptions
{
    public const string Section = "Resend";

    /// <summary>API Key do Resend (re_xxxxxxxxxxxx).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Endereço remetente verificado no Resend. Ex: newsletter@webriders.com.br</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Nome do remetente exibido no cliente de e-mail.</summary>
    public string FromName { get; set; } = "Webriders";
}

// ══════════════════════════════════════════════════════════════
//  Interface
// ══════════════════════════════════════════════════════════════
public interface IResendEmailService
{
    /// <summary>
    /// Envia um e-mail via Resend API.
    /// Retorna (sucesso, messageId, erro).
    /// </summary>
    Task<(bool Success, string? MessageId, string? Error)> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}

// ══════════════════════════════════════════════════════════════
//  Implementação
// ══════════════════════════════════════════════════════════════
public class ResendEmailService : IResendEmailService
{
    private readonly HttpClient _http;
    private readonly ResendOptions _opts;

    public ResendEmailService(HttpClient http, IOptions<ResendOptions> opts)
    {
        _http = http;
        _opts = opts.Value;

        _http.BaseAddress = new Uri("https://api.resend.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
    }

    public async Task<(bool Success, string? MessageId, string? Error)> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        var payload = new
        {
            from = $"{_opts.FromName} <{_opts.FromEmail}>",
            to = new[] { $"{toName} <{toEmail}>" },
            subject,
            html = htmlBody
        };

        var json = JsonSerializer.Serialize(payload);// Forma correta:
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync("emails", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                var msgId = doc.RootElement
                               .GetProperty("id")
                               .GetString();
                return (true, msgId, null);
            }

            return (false, null, $"HTTP {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

// ══════════════════════════════════════════════════════════════
//  Interface
// ══════════════════════════════════════════════════════════════
public interface INewsletterQueueService
{
    /// <summary>Enfileira um disparo para o post recém-publicado (idempotente).</summary>
    Task EnqueueAsync(int postId, CancellationToken ct = default);

    /// <summary>Retorna todos os itens com status Pending para o worker processar.</summary>
    Task<List<NewsletterQueue>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Marca o item como "em processamento" e registra tentativa.</summary>
    Task MarkProcessingAsync(int queueId, CancellationToken ct = default);

    /// <summary>Marca como enviado com os contadores finais.</summary>
    Task MarkSentAsync(int queueId, int sent, int failed, CancellationToken ct = default);

    /// <summary>Marca como falhou (após max tentativas).</summary>
    Task MarkFailedAsync(int queueId, string error, CancellationToken ct = default);

    /// <summary>Retorna o histórico de disparos para o painel admin.</summary>
    Task<List<NewsletterQueue>> GetHistoryAsync(int take = 20, CancellationToken ct = default);
}

// ══════════════════════════════════════════════════════════════
//  Implementação
// ══════════════════════════════════════════════════════════════
public class NewsletterQueueService : INewsletterQueueService
{
    private readonly AppDbContext _db;

    public NewsletterQueueService(AppDbContext db) => _db = db;

    public async Task EnqueueAsync(int postId, CancellationToken ct = default)
    {
        // Idempotente: ignora se já existir entrada para este post
        var exists = await _db.NewsletterQueue
            .AnyAsync(q => q.PostId == postId, ct);

        if (exists) return;

        _db.NewsletterQueue.Add(new NewsletterQueue
        {
            PostId = postId,
            Status = NewsletterQueueStatus.Pending,
            QueuedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<NewsletterQueue>> GetPendingAsync(CancellationToken ct = default)
        => await _db.NewsletterQueue
            .Include(q => q.Post)
                .ThenInclude(p => p.Category)
            .Include(q => q.Post)
                .ThenInclude(p => p.Author)
            .Where(q => q.Status == NewsletterQueueStatus.Pending && q.Attempts < 3)
            .OrderBy(q => q.QueuedAt)
            .ToListAsync(ct);

    public async Task MarkProcessingAsync(int queueId, CancellationToken ct = default)
    {
        var item = await _db.NewsletterQueue.FindAsync([queueId], ct)
                   ?? throw new InvalidOperationException($"Queue item {queueId} não encontrado.");

        item.Status = NewsletterQueueStatus.Processing;
        item.ProcessingStartedAt = DateTime.UtcNow;
        item.Attempts += 1;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkSentAsync(int queueId, int sent, int failed, CancellationToken ct = default)
    {
        var item = await _db.NewsletterQueue.FindAsync([queueId], ct)
                   ?? throw new InvalidOperationException($"Queue item {queueId} não encontrado.");

        item.Status = NewsletterQueueStatus.Sent;
        item.ProcessedAt = DateTime.UtcNow;
        item.SentCount = sent;
        item.FailedCount = failed;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(int queueId, string error, CancellationToken ct = default)
    {
        var item = await _db.NewsletterQueue.FindAsync([queueId], ct)
                   ?? throw new InvalidOperationException($"Queue item {queueId} não encontrado.");

        item.Status = NewsletterQueueStatus.Failed;
        item.ProcessedAt = DateTime.UtcNow;
        item.ErrorMessage = error[..Math.Min(error.Length, 2000)];
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<NewsletterQueue>> GetHistoryAsync(int take = 20, CancellationToken ct = default)
        => await _db.NewsletterQueue
            .Include(q => q.Post)
            .OrderByDescending(q => q.QueuedAt)
            .Take(take)
            .ToListAsync(ct);
}

/// <summary>
/// Gera o HTML do e-mail de newsletter.
/// Inline CSS para máxima compatibilidade com clientes de e-mail.
/// </summary>
public interface IEmailTemplateService
{
    string BuildNewPostEmail(Post post, NewsletterSubscriber subscriber, BlogSettings settings);
}

public class EmailTemplateService : IEmailTemplateService
{
    public string BuildNewPostEmail(Post post, NewsletterSubscriber subscriber, BlogSettings settings)
    {
        var blogUrl    = settings.BlogUrl?.TrimEnd('/') ?? "https://webriders.com.br";
        var postUrl    = $"{blogUrl}/post/{post.Slug}";
        var unsubUrl   = $"{blogUrl}/newsletter/cancelar?email={Uri.EscapeDataString(subscriber.Email)}";
        var imgTag     = !string.IsNullOrEmpty(post.FeaturedImage)
            ? $@"<img src=""{post.FeaturedImage}"" alt=""{Escape(post.Title)}""
                      style=""width:100%;max-height:340px;object-fit:cover;display:block;border:0;"" />"
            : string.Empty;

        var categoryName = post.Category?.Name ?? "Novidades";
        var authorName   = post.Author?.DisplayName ?? "Equipe Webriders";
        var readingTime  = post.ReadingTime > 0 ? $"{post.ReadingTime} min de leitura" : "";
        var pubDate      = (post.PublishedAt ?? DateTime.UtcNow).ToString("dd 'de' MMMM 'de' yyyy",
                            new System.Globalization.CultureInfo("pt-BR"));

        return $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>{Escape(post.Title)}</title>
        </head>
        <body style="margin:0;padding:0;background:#0f0f0f;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;">

          <!-- Wrapper -->
          <table width="100%" cellpadding="0" cellspacing="0" border="0"
                 style="background:#0f0f0f;padding:32px 0;">
            <tr>
              <td align="center">

                <!-- Card -->
                <table width="620" cellpadding="0" cellspacing="0" border="0"
                       style="max-width:620px;width:100%;background:#1a1a1a;border:1px solid #2a2a2a;">

                  <!-- Header -->
                  <tr>
                    <td style="padding:28px 32px;border-bottom:3px solid #ff3c00;">
                      <table width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td>
                            <span style="font-family:'Helvetica Neue',sans-serif;
                                         font-size:22px;font-weight:900;
                                         color:#ffffff;letter-spacing:1px;">
                              {Escape(settings.BlogName ?? "Webriders")}<span style="color:#ff3c00;">.</span>
                            </span>
                          </td>
                          <td align="right">
                            <span style="font-size:11px;color:#666;letter-spacing:2px;
                                         text-transform:uppercase;">Newsletter</span>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Imagem destaque -->
                  {imgTag}

                  <!-- Categoria badge -->
                  <tr>
                    <td style="padding:24px 32px 0;">
                      <span style="display:inline-block;background:#ff3c00;color:#fff;
                                   font-size:11px;font-weight:700;letter-spacing:2px;
                                   text-transform:uppercase;padding:4px 12px;">
                        {Escape(categoryName)}
                      </span>
                    </td>
                  </tr>

                  <!-- Título -->
                  <tr>
                    <td style="padding:16px 32px 8px;">
                      <h1 style="margin:0;font-size:28px;font-weight:900;
                                 color:#ffffff;line-height:1.15;
                                 font-family:'Helvetica Neue',sans-serif;">
                        {Escape(post.Title)}
                      </h1>
                    </td>
                  </tr>

                  <!-- Meta -->
                  <tr>
                    <td style="padding:0 32px 16px;">
                      <span style="font-size:12px;color:#888;">
                        {Escape(authorName)} &nbsp;·&nbsp; {pubDate}
                        {(readingTime != "" ? $" &nbsp;·&nbsp; {readingTime}" : "")}
                      </span>
                    </td>
                  </tr>

                  <!-- Excerpt -->
                  <tr>
                    <td style="padding:0 32px 28px;">
                      <p style="margin:0;font-size:15px;color:#aaa;line-height:1.7;">
                        {Escape(post.Excerpt ?? "")}
                      </p>
                    </td>
                  </tr>

                  <!-- CTA -->
                  <tr>
                    <td style="padding:0 32px 36px;">
                      <a href="{postUrl}"
                         style="display:inline-block;background:#ff3c00;color:#ffffff;
                                font-size:14px;font-weight:700;text-decoration:none;
                                padding:14px 32px;letter-spacing:1px;">
                        LER ARTIGO COMPLETO →
                      </a>
                    </td>
                  </tr>

                  <!-- Divider -->
                  <tr>
                    <td style="border-top:1px solid #2a2a2a;padding:20px 32px;">
                      <p style="margin:0;font-size:11px;color:#555;line-height:1.6;">
                        Você está recebendo este e-mail porque se inscreveu na newsletter
                        do {Escape(settings.BlogName ?? "Webriders")}.<br>
                        <a href="{unsubUrl}" style="color:#ff3c00;text-decoration:none;">
                          Cancelar inscrição
                        </a>
                      </p>
                    </td>
                  </tr>

                </table>
                <!-- /Card -->

              </td>
            </tr>
          </table>

        </body>
        </html>
        """;
    }

    private static string Escape(string s) =>
        System.Web.HttpUtility.HtmlEncode(s);
}