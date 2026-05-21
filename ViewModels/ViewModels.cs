using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ThrottleBlog.Models;

namespace ThrottleBlog.ViewModels;

// ─────────────────────────────────────────────
//  AUTH
// ─────────────────────────────────────────────
public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o usuário")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool   RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

// ─────────────────────────────────────────────
//  BLOG PÚBLICO — HOME
// ─────────────────────────────────────────────
public class HomeViewModel
{
    public BlogSettings    Settings      { get; set; } = null!;
    public List<Post>      FeaturedPosts { get; set; } = new();
    public List<Post>      LatestPosts   { get; set; } = new();
    public List<Post>      MostRead      { get; set; } = new();
    public List<Category>  Categories    { get; set; } = new();
    public List<Tag>       PopularTags   { get; set; } = new();
}

// ─────────────────────────────────────────────
//  BLOG PÚBLICO — POST DETAIL
// ─────────────────────────────────────────────
public class PostDetailViewModel
{
    public Post         Post         { get; set; } = null!;
    public BlogSettings Settings     { get; set; } = null!;
    public string       ContentHtml  { get; set; } = string.Empty;
    public List<Post>   RelatedPosts { get; set; } = new();
    public List<Post>   MostRead     { get; set; } = new();
}

// ─────────────────────────────────────────────
//  BLOG PÚBLICO — CATEGORY PAGE
// ─────────────────────────────────────────────
public class CategoryViewModel
{
    public Category            Category   { get; set; } = null!;
    public BlogSettings        Settings   { get; set; } = null!;
    public List<Post>          Posts      { get; set; } = new();
    public List<Category>      AllCategories { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = null!;
}

// ─────────────────────────────────────────────
//  BLOG PÚBLICO — SEARCH
// ─────────────────────────────────────────────
public class SearchViewModel
{
    public string              Query      { get; set; } = string.Empty;
    public List<Post>          Posts      { get; set; } = new();
    public int                 Total      { get; set; }
    public BlogSettings        Settings   { get; set; } = null!;
    public PaginationViewModel Pagination { get; set; } = null!;
}

// ─────────────────────────────────────────────
//  SHARED — PAGINATION
// ─────────────────────────────────────────────
public class PaginationViewModel
{
    public int  CurrentPage { get; set; }
    public int  TotalPages  { get; set; }
    public int  TotalItems  { get; set; }
    public int  PageSize    { get; set; }
    public bool HasPrev     => CurrentPage > 1;
    public bool HasNext     => CurrentPage < TotalPages;
}

// ─────────────────────────────────────────────
//  ADMIN — DASHBOARD
// ─────────────────────────────────────────────
public class DashboardViewModel
{
    public int  TotalPosts       { get; set; }
    public int  PublishedPosts   { get; set; }
    public int  DraftPosts       { get; set; }
    public int  TotalCategories  { get; set; }
    public int  PendingComments  { get; set; }
    public List<Post>           RecentPosts       { get; set; } = new();
    public List<CategoryCount>  CategoryBreakdown { get; set; } = new();
    public List<DailyActivity>  WeeklyActivity    { get; set; } = new();
}

public record CategoryCount(string Name, int Count);
public record DailyActivity(string DayLabel, int Count);

// ─────────────────────────────────────────────
//  ADMIN — POSTS LIST
// ─────────────────────────────────────────────
public class PostListViewModel
{
    public List<Post>          Posts          { get; set; } = new();
    public List<Category>      Categories     { get; set; } = new();
    public PaginationViewModel Pagination     { get; set; } = null!;
    public string? SearchQuery    { get; set; }
    public string? CategoryFilter { get; set; }
    public string? StatusFilter   { get; set; }
}

// ─────────────────────────────────────────────
//  ADMIN — POST FORM (CREATE / EDIT)
// ─────────────────────────────────────────────
public class PostFormViewModel : IValidatableObject
{
    private static readonly Regex ImagePathRegex = new(@"^/[\w\-./]+$", RegexOptions.Compiled);

    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Excerpt { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? FeaturedImage { get; set; }

    [Range(1, 120)]
    public int ReadingTime { get; set; } = 5;

    [Required]
    public PostStatus Status { get; set; } = PostStatus.Draft;

    public bool IsFeatured { get; set; }

    [Required(ErrorMessage = "Selecione uma categoria")]
    public int CategoryId { get; set; }

    public string TagsRaw { get; set; } = string.Empty;

    public List<Category> Categories { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(FeaturedImage) &&
            !Uri.TryCreate(FeaturedImage, UriKind.Absolute, out _) &&
            !ImagePathRegex.IsMatch(FeaturedImage))
        {
            yield return new ValidationResult(
                "Informe uma URL válida ou um caminho começando com / (ex.: /uploads/...).",
                [nameof(FeaturedImage)]);
        }

        if (Status == PostStatus.Published && string.IsNullOrWhiteSpace(FeaturedImage))
        {
            yield return new ValidationResult(
                "Imagem de destaque é obrigatória para publicar.",
                [nameof(FeaturedImage)]);
        }
    }
}

// ─────────────────────────────────────────────
//  ADMIN — CATEGORIES
// ─────────────────────────────────────────────
public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Url(ErrorMessage = "Informe uma URL válida")]
    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }
    public int PostCount { get; set; }
}

// ─────────────────────────────────────────────
//  ADMIN — SETTINGS
// ─────────────────────────────────────────────
public class SettingsViewModel
{
    [Required]
    public string BlogName          { get; set; } = string.Empty;
    public string Tagline           { get; set; } = string.Empty;
    public string TopBarText        { get; set; } = string.Empty;
    public string BlogUrl           { get; set; } = string.Empty;
    public bool   NewsletterEnabled { get; set; }
    public bool   CommentsEnabled   { get; set; }
    public bool   MaintenanceMode   { get; set; }
    public bool   TickerEnabled     { get; set; }
    public bool   FeaturedEnabled   { get; set; }
    public string? Instagram        { get; set; }
    public string? YouTube          { get; set; }
    public string? Twitter          { get; set; }
    public string? LinkedIn         { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    public string UserName       { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? NewPassword   { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "As senhas não coincidem")]
    public string? ConfirmPassword { get; set; }
}

// ─────────────────────────────────────────────
//  ADMIN — MEDIA
// ─────────────────────────────────────────────
public class MediaViewModel
{
    public List<ThrottleBlog.Models.UploadedFile> Files { get; set; } = new();
}

// ─────────────────────────────────────────────
//  ADMIN — COMMENTS
// ─────────────────────────────────────────────
public class CommentsViewModel
{
    public List<Comment> Comments    { get; set; } = new();
    public string?       StatusFilter{ get; set; }
    public int           PendingCount{ get; set; }
}

// ─────────────────────────────────────────────
//  NEWSLETTER (público)
// ─────────────────────────────────────────────
public class NewsletterPageViewModel
{
    public BlogSettings Settings { get; set; } = null!;
    public int          CommonCount  { get; set; }
    public int          PremiumCount { get; set; }
}

public class NewsletterFormViewModel
{
    [Required(ErrorMessage = "Informe seu nome")]
    [StringLength(120)]
    public string Name  { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail"), EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = string.Empty;
}

public class PremiumNewsletterFormViewModel
{
    [Required(ErrorMessage = "Informe seu nome")]
    [StringLength(120)]
    public string Name  { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail"), EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu telefone")]
    [Phone(ErrorMessage = "Telefone inválido")]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;
}

public class AdminNewsletterListViewModel
{
    public List<NewsletterSubscriber> Subscribers  { get; set; } = new();
    public string?                    SearchQuery  { get; set; }
    public string?                    TierFilter   { get; set; }
    public string?                    StatusFilter { get; set; }
    public int                        CommonTotal  { get; set; }
    public int                        PremiumTotal { get; set; }
    public PaginationViewModel        Pagination   { get; set; } = null!;
}

// ─────────────────────────────────────────────
//  COMMENT FORM (público)
// ─────────────────────────────────────────────
public class CommentFormViewModel
{
    public int PostId { get; set; }

    [Required(ErrorMessage = "Informe seu nome")]
    [StringLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string AuthorEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escreva um comentário")]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;
}