// ============================================================================
// 1. NAMESPACES E DEPENDÊNCIAS
// ============================================================================
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Models;

namespace ThrottleBlog.Data;

/// <summary>
/// Contexto principal de dados do ecossistema ThrottleBlog.
/// Herda de IdentityDbContext para gerenciar automaticamente as tabelas de usuários e credenciais.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // ============================================================================
    // 2. CONSTRUTOR
    // ============================================================================
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ============================================================================
    // 3. MAPEAMENTO DE TABELAS (DBSETS)
    // ============================================================================

    #region CMS & Núcleo do Conteúdo
    public DbSet<Post>       Posts      { get; set; }
    public DbSet<Category>   Categories { get; set; }
    public DbSet<Tag>        Tags       { get; set; }
    public DbSet<PostTag>    PostTags   { get; set; }
    public DbSet<Comment>    Comments   { get; set; }
    #endregion

    #region Motor de Marketing & Newsletter
    public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }
    public DbSet<NewsletterQueue>      NewsletterQueue       { get; set; }
    public DbSet<NewsletterSendLog>    NewsletterSendLog     { get; set; }
    #endregion

    #region Infraestrutura & Sistema
    public DbSet<BlogSettings>  BlogSettings  { get; set; }
    public DbSet<UploadedFile>  UploadedFiles { get; set; }
    #endregion

    // ============================================================================
    // 4. CONFIGURAÇÃO DE MODELOS (FLUENT API)
    // ============================================================================
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Executa as configurações internas do ASP.NET Core Identity (Roles, Claims, etc.)
        base.OnModelCreating(builder);

        #region CONFIGURAÇÃO N-N (MANY-TO-MANY)
        
        // Relacionamento entre Artigos (Posts) e Tags
        builder.Entity<PostTag>(e =>
        {
            e.HasKey(pt => new { pt.PostId, pt.TagId });

            e.HasOne(pt => pt.Post)
             .WithMany(p => p.PostTags)
             .HasForeignKey(pt => pt.PostId);

            e.HasOne(pt => pt.Tag)
             .WithMany(t => t.PostTags)
             .HasForeignKey(pt => pt.TagId);
        });

        #endregion

        #region ENTIDADES DE CONTEÚDO (CMS)

        // Configuração: Artigos (Posts)
        builder.Entity<Post>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Status).HasConversion<string>(); // Salva o Enum como string no banco

            // Evita a deleção em cascata acidental de categorias com posts associados
            e.HasOne(p => p.Category)
             .WithMany(c => c.Posts)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            // Evita a deleção em cascata de posts caso o autor seja excluído
            e.HasOne(p => p.Author)
             .WithMany(u => u.Posts)
             .HasForeignKey(p => p.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração: Categorias
        builder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        // Configuração: Tags
        builder.Entity<Tag>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
        });

        // Configuração: Comentários
        builder.Entity<Comment>(e =>
        {
            e.Property(c => c.Status).HasConversion<string>();

            // Se o post cair, os comentários somem junto (Cascade)
            e.HasOne(c => c.Post)
             .WithMany(p => p.Comments)
             .HasForeignKey(c => c.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            // Se o usuário dono deletar a conta, o comentário permanece anônimo (SetNull)
            e.HasOne(c => c.User)
             .WithMany(u => u.Comments)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        #endregion

        #region ENTIDADES DE MARKETING (NEWSLETTER)

        // Configuração: Assinantes
        builder.Entity<NewsletterSubscriber>(e =>
        {
            e.ToTable("NewsletterSubscribers");
            e.HasKey(e => e.Id);
            e.HasIndex(e => e.Email).IsUnique();
            e.Property(e => e.Tier).HasConversion<string>();
        });

        // Configuração: Fila de Disparos (Consolidado)
        builder.Entity<NewsletterQueue>(e =>
        {
            e.ToTable("NewsletterQueue");
            e.HasKey(e => e.Id);

            e.HasOne(q => q.Post)
             .WithMany()
             .HasForeignKey(q => q.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(q => q.Status);            // Otimiza consultas do background worker (ex: buscar apenas 'Pending')
            e.HasIndex(q => q.PostId).IsUnique(); // Regra de negócio: Apenas 1 disparo agendado por Post
        });

        // Configuração: Histórico / Logs de Envio (Consolidado)
        builder.Entity<NewsletterSendLog>(e =>
        {
            e.ToTable("NewsletterSendLog");
            e.HasKey(e => e.Id);

            e.HasOne(l => l.Queue)
             .WithMany()
             .HasForeignKey(l => l.NewsletterQueueId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Subscriber)
             .WithMany()
             .HasForeignKey(l => l.SubscriberId)
             .OnDelete(DeleteBehavior.Cascade);

            // Index composto para monitorar entregas e evitar duplicidade de e-mail na mesma campanha
            e.HasIndex(l => new { l.NewsletterQueueId, l.SubscriberId });
        });

        #endregion

        #region ALIMENTAÇÃO INICIAL DE DADOS (DATA SEEDING)

        // Inicializa o registro de configurações globais com ID fixo = 1
        builder.Entity<BlogSettings>().HasData(new BlogSettings { Id = 1 });

        // Semeia as categorias iniciais do portal
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Reviews",   Slug = "reviews",   SortOrder = 1, ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&q=70" },
            new Category { Id = 2, Name = "Segurança", Slug = "seguranca", SortOrder = 2, ImageUrl = "https://images.unsplash.com/photo-1609630875171-b1321377ee65?w=400&q=70" },
            new Category { Id = 4, Name = "Rotas",     Slug = "rotas",     SortOrder = 4, ImageUrl = "https://images.unsplash.com/photo-1558981359-219d6364c9c8?w=400&q=70" },
            new Category { Id = 6, Name = "Eventos",   Slug = "eventos",   SortOrder = 6, ImageUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=400&q=70" }
        );

        #endregion
    }
}