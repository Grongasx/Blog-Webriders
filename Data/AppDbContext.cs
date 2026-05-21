using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Models;

namespace ThrottleBlog.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Post>                 Posts                 { get; set; }
    public DbSet<Category>             Categories            { get; set; }
    public DbSet<Tag>                  Tags                  { get; set; }
    public DbSet<PostTag>              PostTags              { get; set; }
    public DbSet<Comment>              Comments              { get; set; }
    public DbSet<BlogSettings>         BlogSettings          { get; set; }
    public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }
    public DbSet<UploadedFile>         UploadedFiles         { get; set; }
    public DbSet<NewsletterQueue>      NewsletterQueue       { get; set; }
    public DbSet<NewsletterSendLog>    NewsletterSendLog     { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PostTag>().HasKey(pt => new { pt.PostId, pt.TagId });
        builder.Entity<PostTag>().HasOne(pt => pt.Post).WithMany(p => p.PostTags).HasForeignKey(pt => pt.PostId);
        builder.Entity<PostTag>().HasOne(pt => pt.Tag).WithMany(t => t.PostTags).HasForeignKey(pt => pt.TagId);

        builder.Entity<Post>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Status).HasConversion<string>();
            e.HasOne(p => p.Category).WithMany(c => c.Posts).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Author).WithMany(u => u.Posts).HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comment>(e =>
        {
            e.Property(c => c.Status).HasConversion<string>();
            e.HasOne(c => c.Post).WithMany(p => p.Comments).HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        builder.Entity<NewsletterQueue>(e =>
        {
            e.HasOne(q => q.Post)
            .WithMany()
            .HasForeignKey(q => q.PostId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(q => q.Status);            // filtro rápido por Pending
            e.HasIndex(q => q.PostId).IsUnique(); // 1 disparo por post
        });

        builder.Entity<NewsletterSendLog>(e =>
        {
            e.HasOne(l => l.Queue)
            .WithMany()
            .HasForeignKey(l => l.NewsletterQueueId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Subscriber)
            .WithMany()
            .HasForeignKey(l => l.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => new { l.NewsletterQueueId, l.SubscriberId });
        });

        builder.Entity<NewsletterSubscriber>(entity =>
        {
            entity.ToTable("NewsletterSubscribers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tier).HasConversion<string>();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configuração NewsletterQueue
        builder.Entity<NewsletterQueue>(entity =>
        {
            entity.ToTable("NewsletterQueue");
            entity.HasKey(e => e.Id);
        });

        // Configuração NewsletterSendLog
        builder.Entity<NewsletterSendLog>(entity =>
        {
            entity.ToTable("NewsletterSendLog");
            entity.HasKey(e => e.Id);
        });
        

        builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        builder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
        builder.Entity<BlogSettings>().HasData(new BlogSettings { Id = 1 });

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Reviews",   Slug = "reviews",   SortOrder = 1, ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&q=70" },
            new Category { Id = 2, Name = "Segurança", Slug = "seguranca", SortOrder = 2, ImageUrl = "https://images.unsplash.com/photo-1609630875171-b1321377ee65?w=400&q=70" },
            new Category { Id = 4, Name = "Rotas",     Slug = "rotas",     SortOrder = 4, ImageUrl = "https://images.unsplash.com/photo-1558981359-219d6364c9c8?w=400&q=70" },
            new Category { Id = 6, Name = "Eventos",   Slug = "eventos",   SortOrder = 6, ImageUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=400&q=70" }
        );
    }
}