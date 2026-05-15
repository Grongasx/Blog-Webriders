using ThrottleBlog.Models;

namespace ThrottleBlog.ViewModels;

/// <summary>
/// ViewModel compartilhado pelas páginas de categoria dedicadas:
/// Novidades, Rotas, Reviews e Eventos.
/// </summary>
public class CategoryPageViewModel
{
    /// <summary>Post mais recente exibido como hero full-width.</summary>
    public Post? HeroPost { get; set; }

    /// <summary>
    /// Posts em destaque (IsFeatured = true) para seções especiais:
    /// — Novidades: ticker lateral
    /// — Rotas: trio de cards grandes
    /// — Reviews: grid dupla de destaque
    /// — Eventos: agenda de próximos eventos
    /// </summary>
    public IReadOnlyList<Post> FeaturedPosts { get; set; } = [];

    /// <summary>
    /// Posts recentes para o ticker animado (Novidades).
    /// Nas outras categorias pode ficar vazio.
    /// </summary>
    public IReadOnlyList<Post> TickerPosts { get; set; } = [];

    /// <summary>Grid paginado de todos os posts da categoria.</summary>
    public IReadOnlyList<Post> Posts { get; set; } = [];

    /// <summary>Metadados da categoria (nome, slug, imagem).</summary>
    public Category Category { get; set; } = default!;

    /// <summary>Dados de paginação para o grid principal.</summary>
    public PaginationViewModel Pagination { get; set; } = new();

    /// <summary>Configurações gerais do blog (necessário para o _Layout).</summary>
    public BlogSettings Settings { get; set; } = default!;
}
