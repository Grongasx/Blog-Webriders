using Microsoft.AspNetCore.Identity;

namespace ThrottleBlog.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl  { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Post>    Posts    { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public class Category
{
    public int     Id               { get; set; }
    public string  Name             { get; set; } = string.Empty;
    public string  Slug             { get; set; } = string.Empty;
    public string? ControllerName   {get; set; }
    public string? ImageUrl         { get; set; }
    public int     SortOrder        { get; set; }
    public ICollection<Post> Posts  { get; set; } = new List<Post>();
}

public enum PostStatus { Draft, Published }

public class Post
{
    public int        Id            { get; set; }
    public string     Title         { get; set; } = string.Empty;
    public string     Slug          { get; set; } = string.Empty;
    public string     Excerpt       { get; set; } = string.Empty;
    public string     Content       { get; set; } = string.Empty;
    public string?    FeaturedImage { get; set; }
    public string?    GalleryImages { get; set; }
    public int        ReadingTime   { get; set; } = 5;
    public PostStatus Status        { get; set; } = PostStatus.Draft;
    public bool       IsFeatured    { get; set; }
    public int        ViewCount     { get; set; }
    public DateTime   CreatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime   UpdatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime?  PublishedAt   { get; set; }
    public int            CategoryId { get; set; }
    public string         AuthorId   { get; set; } = string.Empty;
    public Category        Category  { get; set; } = null!;
    public ApplicationUser Author    { get; set; } = null!;
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public class Tag
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}

public class PostTag
{
    public int  PostId { get; set; }
    public Post Post   { get; set; } = null!;
    public int  TagId  { get; set; }
    public Tag  Tag    { get; set; } = null!;
}

public enum CommentStatus { Pending, Approved, Spam }

public class Comment
{
    public int           Id          { get; set; }
    public string        AuthorName  { get; set; } = string.Empty;
    public string        AuthorEmail { get; set; } = string.Empty;
    public string        Body        { get; set; } = string.Empty;
    public CommentStatus Status      { get; set; } = CommentStatus.Pending;
    public DateTime      CreatedAt   { get; set; } = DateTime.UtcNow;
    public int             PostId    { get; set; }
    public Post            Post      { get; set; } = null!;
    public string?         UserId    { get; set; }
    public ApplicationUser? User     { get; set; }
}

public class BlogSettings
{
    public int    Id                { get; set; } = 1;
    public string BlogName          { get; set; } = "THROTTLE";
    public string Tagline           { get; set; } = "O maior blog de motociclismo do Brasil";
    public string TopBarText        { get; set; } = "🏍️ Nova edição disponível";
    public string BlogUrl           { get; set; } = "https://throttle.com.br";
    public bool   NewsletterEnabled { get; set; } = true;
    public bool   CommentsEnabled   { get; set; }
    public bool   MaintenanceMode   { get; set; }
    public bool   TickerEnabled     { get; set; } = true;
    public bool   FeaturedEnabled   { get; set; } = true;
    public string? Instagram        { get; set; }
    public string? YouTube          { get; set; }
    public string? Twitter          { get; set; }
    public string? LinkedIn         { get; set; }
}

public enum NewsletterTier
{
    Common  = 0,
    Premium = 1
}

public class NewsletterSubscriber
{
    public int            Id           { get; set; }
    public string         Name         { get; set; } = string.Empty;
    public string         Email        { get; set; } = string.Empty;
    public string?        Phone        { get; set; }
    public NewsletterTier Tier         { get; set; } = NewsletterTier.Common;
    public DateTime       SubscribedAt { get; set; } = DateTime.UtcNow;
    public bool           IsActive     { get; set; } = true;
}

public class UploadedFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
}

public class NewsletterQueue
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = default!;
    public NewsletterQueueStatus Status { get; set; } = NewsletterQueueStatus.Pending;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int Attempts { get; set; }
}

public enum NewsletterQueueStatus
{
    Pending = 0,   // aguardando o worker
    Processing = 1,   // worker em execução
    Sent = 2,   // concluído com sucesso
    Failed = 3    // falhou após max tentativas
}

public class NewsletterSendLog
{
    public long Id { get; set; } 
    public int NewsletterQueueId { get; set; }
    public NewsletterQueue Queue { get; set; } = default!;
    public int SubscriberId { get; set; }
    public NewsletterSubscriber Subscriber { get; set; } = default!;
    public bool Success { get; set; }
    public string? ResendMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
 