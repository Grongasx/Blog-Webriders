using Microsoft.EntityFrameworkCore;
using ThrottleBlog.Data;
using ThrottleBlog.Models;

namespace ThrottleBlog.Infrastructure;

/// <summary>
/// Popula as categorias padrão do Webriders caso ainda não existam.
/// Chame em Program.cs: await WebridersCategorySeeder.SeedAsync(app);
/// </summary>
public static class WebridersCategorySeeder
{
    private static readonly (string Name, string Slug, int Order)[] _defaults =
    [
        ("Novidades", "novidades", 1),
        ("Rotas",     "rotas",     2),
        ("Reviews",   "reviews",   3),
        ("Eventos",   "eventos",   4),
    ];

    public static async Task SeedAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        foreach (var (name, slug, order) in _defaults)
        {
            if (!await db.Categories.AnyAsync(c => c.Slug == slug))
            {
                db.Categories.Add(new Category
                {
                    Name      = name,
                    Slug      = slug,
                    SortOrder = order
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
