// ============================================================================
// 1. NAMESPACES E DEPENDÊNCIAS
// ============================================================================
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Data;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.Infrastructure;
using ThrottleBlog.Controllers;

// ============================================================================
// 2. INICIALIZAÇÃO DO BUILDER
// ============================================================================
var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 3. CONFIGURAÇÃO DE SERVIÇOS (DI CONTAINER)
// ============================================================================

#region Core MVC, Cache e Clientes HTTP

// Ativa o suporte a Controllers e Views (Padrão MVC)
builder.Services.AddControllersWithViews();

// Ativa o cache em memória interna
builder.Services.AddMemoryCache();

// Configuração do HttpClient customizado para o Web Scraper
builder.Services.AddHttpClient("scraper", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
        "AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(10);
});

#endregion

#region Banco de Dados (Entity Framework)

// Configuração do contexto do banco de dados utilizando PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

#endregion

#region Autenticação e Identidade (ASP.NET Core Identity)

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    // Regras de Complexidade de Senha
    opt.Password.RequiredLength         = 8;
    opt.Password.RequireDigit           = false;
    opt.Password.RequireUppercase       = false;
    opt.Password.RequireNonAlphanumeric = false;

    // Regras de Bloqueio (Lockout) por tentativas incorretas
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(10);

    // Configurações do Usuário
    opt.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configurações personalizadas para os Cookies de Autenticação
builder.Services.ConfigureApplicationCookie(opt =>
{
    // Redirecionamentos customizados da área administrativa
    opt.LoginPath = "/admin/login";
    opt.AccessDeniedPath = "/admin/acesso-negado";
    
    // Ciclo de vida do Cookie
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    opt.SlidingExpiration = true;
    
    // Segurança e compatibilidade do Cookie
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax; 
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

#endregion

#region Serviços de Negócio (Camada Application / Domain)

// Serviços principais do Blog
builder.Services.AddScoped<IPostService,         PostService>();
builder.Services.AddScoped<ICategoryService,    CategoryService>();
builder.Services.AddScoped<ISettingsService,    SettingsService>();
builder.Services.AddScoped<IDashboardService,   DashboardService>();
builder.Services.AddScoped<INewsletterService,  NewsletterService>();
builder.Services.AddScoped<ICommentService,     CommentService>();

// Serviços de Infraestrutura e Renderização
builder.Services.AddScoped<IMarkdownRenderer,   MarkdownRenderer>();
builder.Services.AddScoped<IImageUploadService, LocalImageUploadService>();

// Serviços específicos do Scraper de Produtos
builder.Services.AddScoped<IProductScraperService, ProductScraperService>();
builder.Services.AddScoped<IProductBlockRenderer,  ProductBlockRenderer>();

#endregion

#region Gerenciamento de Sessão

// Ativa o suporte a sessões (utilizado para TempData e mensagens flash)
builder.Services.AddSession(opt => opt.IdleTimeout = TimeSpan.FromMinutes(30));

#endregion

// ============================================================================
// 4. CONSTRUÇÃO DA APLICAÇÃO (BUILD)
// ============================================================================
var app = builder.Build();

// ============================================================================
// 5. PROCESSAMENTO DE MIDDLEWARES (HTTP REQUEST PIPELINE)
// ============================================================================

// Tratamento de Erros e Segurança globais baseados no ambiente
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro/500");
    app.UseHsts();
}

// Captura status codes de erro (ex: 404) e redireciona internamente
app.UseStatusCodePagesWithReExecute("/erro/{0}");

// Redirecionamento HTTPS e entrega de arquivos estáticos (wwwroot)
app.UseHttpsRedirection();
app.UseStaticFiles();

// Biblioteca do Loading...
app.UseStaticFiles();

// Ativa o sistema de roteamento
app.UseRouting();

// Middleware customizado do sistema
app.UseMiddleware<MaintenanceMiddleware>();

// Segurança: Autenticação e Autorização de usuários
app.UseAuthentication();
app.UseAuthorization();

// Ativa o estado de sessão configurado previamente
app.UseSession();

// ============================================================================
// 6. MAPEAMENTO DE ROTAS (ENDPOINTS)
// ============================================================================

// Mapeia os controllers para rotas baseadas em atributos (Attribute Routing)
app.MapControllers(); 

// Rota dedicada para o painel administrativo
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

// Rota padrão do site (Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================================================
// 7. INICIALIZAÇÃO DE DADOS (SEEDERS) E EXECUÇÃO
// ============================================================================

// Executa o Seeder externo de categorias antes de rodar a aplicação
await WebridersCategorySeeder.SeedAsync(app);

// Executa o Seeder local para base de dados e usuário Admin padrão
await SeedAsync(app);

// Inicializa o servidor web de fato
app.Run();

// ============================================================================
// 8. MÉTODOS AUXILIARES / INTERNOS
// ============================================================================

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db    = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Aplica automaticamente qualquer migration pendente no banco de dados
    await db.Database.MigrateAsync();

    // Garante a existência do nível de acesso 'Admin'
    if (!await roles.RoleExistsAsync("Admin"))
        await roles.CreateAsync(new IdentityRole("Admin"));

    // Credenciais do administrador inicial do sistema
    const string adminUser = "admin";
    const string adminPass = "throttle2025";

    // Verifica e cria o usuário administrador inicial se não existir
    if (await users.FindByNameAsync(adminUser) is null)
    {
        var user = new ApplicationUser
        {
            UserName       = adminUser,
            DisplayName    = "Administrador",
            Email          = "admin@throttle.com.br",
            EmailConfirmed = true
        };
        
        var result = await users.CreateAsync(user, adminPass);
        if (result.Succeeded)
        {
            await users.AddToRoleAsync(user, "Admin");
        }
    }
}