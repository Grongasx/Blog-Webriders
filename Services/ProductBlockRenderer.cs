using System.Text;
using System.Text.RegularExpressions;

namespace ThrottleBlog.Services;

/// <summary>
/// Processa a sintaxe customizada  :::produto URL :::  no conteúdo de um post,
/// substituindo cada bloco pelo HTML do card de produto antes de o Markdown ser
/// convertido para HTML.
///
/// A estratégia é:
///   1. No momento da RENDERIZAÇÃO PÚBLICA (HomeController.Detail), esta classe
///      é chamada ANTES de IMarkdownRenderer.ToHtml().
///   2. Para cada bloco :::produto URL :::, fazemos o scraping via IProductScraperService
///      (ou cache em memória) e geramos o HTML do card inline.
///   3. O IMarkdownRenderer recebe o texto já com o HTML do card injetado e o
///      processa normalmente (o Markdig, por exemplo, passa HTML literal intacto).
/// </summary>
public interface IProductBlockRenderer
{
    /// <summary>
    /// Substitui todos os blocos  :::produto URL :::  por HTML de card de produto.
    /// Retorna o conteúdo processado pronto para ser passado ao IMarkdownRenderer.
    /// </summary>
    Task<string> ExpandProductBlocksAsync(string markdownContent);
}

public class ProductBlockRenderer : IProductBlockRenderer
{
    // Regex que casa  :::produto  https://...  :::  (url pode ter query string)
    private static readonly Regex ProductBlockRegex = new(
        @":::produto\s+(https?://[^\s]+)\s+:::",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IProductScraperService _scraper;

    public ProductBlockRenderer(IProductScraperService scraper)
    {
        _scraper = scraper;
    }

    public async Task<string> ExpandProductBlocksAsync(string markdownContent)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
            return markdownContent;

        // Coleta todas as URLs únicas do conteúdo
        var matches = ProductBlockRegex.Matches(markdownContent);
        if (matches.Count == 0)
            return markdownContent;

        // Scraping em paralelo para não bloquear cada URL sequencialmente
        var urls = matches
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .ToList();

        var productDataMap = new Dictionary<string, ProductScraperResult>();
        var tasks = urls.Select(async url =>
        {
            var data = await _scraper.ScrapeAsync(url);
            return (url, data);
        });

        foreach (var (url, data) in await Task.WhenAll(tasks))
            productDataMap[url] = data;

        // Substitui cada bloco pelo HTML gerado
        var result = ProductBlockRegex.Replace(markdownContent, match =>
        {
            var url = match.Groups[1].Value.Trim();
            if (!productDataMap.TryGetValue(url, out var p))
                return string.Empty;

            return BuildProductCardHtml(p);
        });

        return result;
    }

    // ── HTML do card — mesma aparência do preview do Admin ──────────────────
    private static string BuildProductCardHtml(ProductScraperResult p)
    {
        var sb = new StringBuilder();

        // Variantes
        var variantBadges = new StringBuilder();
        foreach (var v in p.Variations)
            variantBadges.Append(
                $"<span class=\"product-variant-badge\">{Encode(v)}</span>");

        sb.Append(
            $"""
            <div class="product-block-card">
              <img src="{Encode(p.Image)}" class="product-block-img" alt="{Encode(p.Title)}" loading="lazy" />
              <div class="product-block-info">
                <div>
                  <div class="product-block-title">{Encode(p.Title)}</div>
                  <div class="product-block-price">{Encode(p.Price)}</div>
                  <div class="product-block-variants">{variantBadges}</div>
                </div>
                <a href="{Encode(p.Url)}" target="_blank" rel="noopener noreferrer"
                   class="product-block-btn">Ver na Loja Oficial →</a>
              </div>
            </div>
            """);

        return sb.ToString();
    }

    private static string Encode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
