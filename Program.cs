using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Data;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.Infrastructure;
using ThrottleBlog.Controllers;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Entity Framework + SQL Server ──────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity ───────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    // Senha
    opt.Password.RequiredLength         = 8;
    opt.Password.RequireDigit           = false;
    opt.Password.RequireUppercase       = false;
    opt.Password.RequireNonAlphanumeric = false;

    // Lockout
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(10);

    // Usuário
    opt.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Redireciona para login do admin (não o padrão /Account/Login)
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/admin/login";
    opt.AccessDeniedPath = "/admin/acesso-negado";
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    opt.SlidingExpiration = true;
    
    // Adicione isso para garantir compatibilidade em desenvolvimento
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax; 
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── Serviços de negócio ─────────────────────────────────
builder.Services.AddScoped<IPostService,       PostService>();
builder.Services.AddScoped<ICategoryService,   CategoryService>();
builder.Services.AddScoped<ISettingsService,   SettingsService>();
builder.Services.AddScoped<IDashboardService,  DashboardService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<ICommentService, CommentService>();

builder.Services.AddTransient<NovidadesController>();
builder.Services.AddTransient<RotasController>();
builder.Services.AddTransient<ReviewsController>();
builder.Services.AddTransient<EventosController>();

// ── Sessão (para mensagens flash em TempData) ───────────
builder.Services.AddSession(opt => opt.IdleTimeout = TimeSpan.FromMinutes(30));

var app = builder.Build();
await WebridersCategorySeeder.SeedAsync(app);
// ── Pipeline ────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/erro/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ── Rotas ───────────────────────────────────────────────
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" }); // Assume que você tem um AdminController

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── Seed: cria admin padrão na primeira execução ────────
await SeedAsync(app);

app.Run();

// ─────────────────────────────────────────────────────────
//  SEED
// ─────────────────────────────────────────────────────────
static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db    = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Aplica migrations pendentes
    await db.Database.MigrateAsync();

    // Cria role Admin
    if (!await roles.RoleExistsAsync("Admin"))
        await roles.CreateAsync(new IdentityRole("Admin"));

    // Cria usuário admin padrão (se não existir)
    const string adminUser = "admin";
    const string adminPass = "throttle2025";

    if (await users.FindByNameAsync(adminUser) is null)
    {
        var user = new ApplicationUser
        {
            UserName    = adminUser,
            DisplayName = "Administrador",
            Email       = "admin@throttle.com.br",
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, adminPass);
        if (result.Succeeded)
            await users.AddToRoleAsync(user, "Admin");
    }
}
