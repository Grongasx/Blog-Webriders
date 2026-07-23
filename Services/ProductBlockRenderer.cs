using System.Text;
using System.Text.RegularExpressions;

namespace ThrottleBlog.Services;

/// <summary>
/// Processa a sintaxe  :::produto URL :::  no conteúdo de um post.
///
/// COMPORTAMENTO:
///   - 1 bloco isolado           → card simples  (.product-block-card)
///   - 2+ blocos CONSECUTIVOS    → carrossel     (.pc-carousel)
///     (separados apenas por espaço/quebra de linha)
///
/// O agrupamento é feito em duas passagens:
///   1. Coleta todas as URLs e faz o scraping em paralelo.
///   2. Substitui sequências consecutivas pelo HTML correto.
/// </summary>
public interface IProductBlockRenderer
{
    Task<string> ExpandProductBlocksAsync(string markdownContent);
}

public class ProductBlockRenderer : IProductBlockRenderer
{
    // Casa um bloco :::produto URL :::
    private static readonly Regex SingleBlockRegex = new(
        @":::produto\s+(https?://[^\s]+)\s+:::",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Casa UMA SEQUÊNCIA de blocos consecutivos (separados só por whitespace)
    private static readonly Regex GroupRegex = new(
        @"(:::produto\s+https?://[^\s]+\s+:::(?:\s*:::produto\s+https?://[^\s]+\s+:::)+)",
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

        var matches = SingleBlockRegex.Matches(markdownContent);
        if (matches.Count == 0)
            return markdownContent;

        // ── 1. Scraping em paralelo de todas as URLs únicas ──────────────
        var urls = matches
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .ToList();

        var tasks = urls.Select(async url =>
        {
            var data = await _scraper.ScrapeAsync(url);
            return (url, data);
        });

        var productMap = new Dictionary<string, ProductScraperResult>();
        foreach (var (url, data) in await Task.WhenAll(tasks))
            productMap[url] = data;

        // Contador LOCAL por execução — sem estado estático, sem race condition
        int localSeed = 0;

        // ── 2. Substitui grupos consecutivos → carrossel ─────────────────
        var processed = GroupRegex.Replace(markdownContent, groupMatch =>
        {
            var urlsInGroup = SingleBlockRegex
                .Matches(groupMatch.Value)
                .Select(m => m.Groups[1].Value.Trim())
                .ToList();

            var products = urlsInGroup
                .Where(u => productMap.ContainsKey(u))
                .Select(u => productMap[u])
                .ToList();

            return BuildCarouselHtml(products, $"pc{localSeed++}");
        });

        // ── 3. Substitui blocos ISOLADOS restantes → card simples ─────────
        processed = SingleBlockRegex.Replace(processed, match =>
        {
            var url = match.Groups[1].Value.Trim();
            return productMap.TryGetValue(url, out var p)
                ? BuildProductCardHtml(p)
                : string.Empty;
        });

        return processed;
    }

    // ── CARROSSEL ────────────────────────────────────────────────────────────
    private static string BuildCarouselHtml(List<ProductScraperResult> products, string id)
    {
        if (products.Count == 0) return string.Empty;
        if (products.Count == 1) return BuildProductCardHtml(products[0]);

        var sb = new StringBuilder();

        sb.Append($"""<div class="pc-carousel" id="{id}" role="region" aria-label="Carrossel de produtos">""");

        // ── Track (faixa deslizável) ──
        sb.Append("""<div class="pc-track-wrap"><ul class="pc-track" aria-live="polite">""");

        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i];

            var variantBadges = string.Concat(
                p.Variations.Select(v => $"""<span class="product-variant-badge">{Encode(v)}</span>"""));

            sb.Append($"""
                <li class="pc-slide" role="group"
                    aria-label="Produto {i + 1} de {products.Count}"
                    aria-hidden="{(i == 0 ? "false" : "true")}">
                  <div class="product-block-card">
                    <img src="{Encode(p.Image)}" class="product-block-img"
                         alt="{Encode(p.Title)}" loading="{(i == 0 ? "eager" : "lazy")}" />
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
                </li>
                """);
        }

        sb.Append("</ul></div>"); // fecha pc-track + pc-track-wrap

        // ── Setas ──
        sb.Append($"""
            <button class="pc-arrow pc-prev" aria-label="Produto anterior" data-carousel="{id}">&#8249;</button>
            <button class="pc-arrow pc-next" aria-label="Próximo produto"  data-carousel="{id}">&#8250;</button>
            """);

        // ── Dots ──
        sb.Append("""<div class="pc-dots" role="tablist">""");
        for (int i = 0; i < products.Count; i++)
            sb.Append($"""
                <button class="pc-dot{(i == 0 ? " active" : "")}"
                        role="tab"
                        aria-selected="{(i == 0 ? "true" : "false")}"
                        aria-label="Ir para produto {i + 1}"
                        data-index="{i}"
                        data-carousel="{id}">
                </button>
                """);
        sb.Append("</div>");

        // ── Contador ──
        sb.Append($"""
            <div class="pc-counter" aria-hidden="true">
              <span class="pc-current">1</span> / <span>{products.Count}</span>
            </div>
            """);

        sb.Append("</div>"); // fecha pc-carousel

        return sb.ToString();
    }

    // ── CARD SIMPLES (1 produto isolado) ────────────────────────────────────
    private static string BuildProductCardHtml(ProductScraperResult p)
    {
        var variantBadges = string.Concat(
            p.Variations.Select(v => $"""<span class="product-variant-badge">{Encode(v)}</span>"""));

        return $"""
            <div class="product-block-card">
              <img src="{Encode(p.Image)}" class="product-block-img"
                   alt="{Encode(p.Title)}" loading="lazy" />
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
            """;
    }

    private static string Encode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}