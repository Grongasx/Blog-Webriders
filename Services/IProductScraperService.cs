using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace ThrottleBlog.Services;

/// <summary>
/// Resultado de scraping de um produto Webriders.
/// </summary>
public record ProductScraperResult(
    string Title,
    string Image,
    string Price,
    IReadOnlyList<string> Variations,
    string Url);

/// <summary>
/// Serviço de scraping de produtos — compartilhado entre AdminController
/// (endpoint /admin/scrape-product usado pelo editor) e ProductBlockRenderer
/// (renderização pública no HomeController).
///
/// Mantém um cache em memória (IMemoryCache) para evitar requisições
/// repetidas para a mesma URL dentro da mesma instância da aplicação.
/// </summary>
public interface IProductScraperService
{
    Task<ProductScraperResult> ScrapeAsync(string url);
}

public class ProductScraperService : IProductScraperService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
    private readonly ILogger<ProductScraperService> _logger;

    // Produtos mudam pouco; cache de 30 min evita re-scraping a cada page view
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public ProductScraperService(
        IHttpClientFactory httpFactory,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        ILogger<ProductScraperService> logger)
    {
        _httpFactory = httpFactory;
        _cache       = cache;
        _logger      = logger;
    }

    public async Task<ProductScraperResult> ScrapeAsync(string url)
    {
        var cacheKey = $"product_scrape:{url}";

        if (_cache.TryGetValue<ProductScraperResult>(cacheKey, out var cached) && cached != null)
            return cached;

        var result = await DoScrapeAsync(url);
        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private async Task<ProductScraperResult> DoScrapeAsync(string url)
    {
        try
        {
            var client = _httpFactory.CreateClient("scraper");
            var html = await client.GetStringAsync(url);

            string ExtractMeta(string property)
            {
                var m = Regex.Match(html,
                    $"<meta[^>]*property=[\"']{property}[\"'][^>]*content=[\"']([^\"']*)[\"']",
                    RegexOptions.IgnoreCase);
                if (!m.Success)
                    m = Regex.Match(html,
                        $"<meta[^>]*content=[\"']([^\"']*)[\"'][^>]*property=[\"']{property}[\"']",
                        RegexOptions.IgnoreCase);
                return m.Success ? System.Web.HttpUtility.HtmlDecode(m.Groups[1].Value) : string.Empty;
            }

            // Título
            string title = ExtractMeta("og:title");
            if (string.IsNullOrWhiteSpace(title))
            {
                var tm = Regex.Match(html, "<h1[^>]*>([^<]*)</h1>", RegexOptions.IgnoreCase);
                title = tm.Success ? tm.Groups[1].Value.Trim() : "Produto Webriders";
            }

            // Imagem
            string image = ExtractMeta("og:image");
            if (string.IsNullOrWhiteSpace(image))
                image = "/img/product-placeholder.jpg";

            // Preço
            string price = ExtractMeta("product:price:amount");
            if (string.IsNullOrWhiteSpace(price))
            {
                var pm = Regex.Match(html,
                    @"(?:class|id)=.*price.*?>\s*(?:R\$\s*)?([0-9.,]+)",
                    RegexOptions.IgnoreCase);
                price = pm.Success ? "R$ " + pm.Groups[1].Value.Trim() : "Consultar valor";
            }
            else
            {
                price = "R$ " + price;
            }

            // Variações
            var variations = new List<string>();
            var variantMatches = Regex.Matches(html, @"<option[^>]*>([^<]+)</option>", RegexOptions.IgnoreCase);
            foreach (Match m in variantMatches)
            {
                var txt = m.Groups[1].Value.Trim();
                if (!txt.Contains("Selecione", StringComparison.OrdinalIgnoreCase)
                    && !txt.Contains("escolha", StringComparison.OrdinalIgnoreCase)
                    && txt.Length < 30
                    && variations.Count < 5)
                {
                    variations.Add(txt);
                }
            }
            if (!variations.Any()) variations.Add("Tamanho Único");

            return new ProductScraperResult(title, image, price, variations, url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao fazer scraping do produto: {Url}", url);
            return new ProductScraperResult(
                "Produto indisponível",
                "/img/product-placeholder.jpg",
                "R$ --",
                new[] { "Padrão" },
                url);
        }
    }
}
