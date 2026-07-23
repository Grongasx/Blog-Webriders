using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;
using ThrottleBlog.Models;
using Ganss.Xss;
using Microsoft.Extensions.Caching.Memory;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador responsável pela exibição interna e processamento de artigos.
/// </summary>
[Route("post")]
public class PostController : Controller
{
    private readonly IPostService          _posts;
    private readonly ISettingsService      _settings;
    private readonly IMarkdownRenderer     _markdown;
    private readonly IProductBlockRenderer _productRenderer;
    private readonly IMemoryCache          _cache;

    public PostController(
        IPostService          posts, 
        ISettingsService      settings, 
        IMarkdownRenderer     markdown, 
        IProductBlockRenderer productRenderer,
        IMemoryCache          cache)
    {
        _posts          = posts;
        _settings       = settings;
        _markdown       = markdown;
        _productRenderer = productRenderer;
        _cache          = cache;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest("O slug não pode ser vazio.");
        }

        var normalizedSlug = slug.ToLowerInvariant().Trim();

        var post = await _posts.GetBySlugAsync(normalizedSlug);
        if (post is null)
        {
            return NotFound();
        }

        var settings = await _settings.GetAsync() ?? new BlogSettings();
        var related = await _posts.GetRelatedAsync(post.Id, post.CategoryId) ?? new List<Post>();
        var mostRead = await _posts.GetMostReadAsync(5) ?? new List<Post>();

        string cacheKey = $"post_html_{normalizedSlug}";
        
        if (!_cache.TryGetValue(cacheKey, out string? cleanHtml))
        {
            // --- CONFIGURAÇÃO DO SANITIZADOR ---
            // --- CONFIGURAÇÃO DO SANITIZADOR ---
            var sanitizer = new HtmlSanitizer();

            // Permissões de Layout
            sanitizer.AllowedAttributes.Add("class");

            // --- PERMISSÕES DE VÍDEO (Iframe e Tags de Video) ---
            sanitizer.AllowedTags.Add("iframe");
            sanitizer.AllowedTags.Add("video");
            sanitizer.AllowedTags.Add("source");

            // Atributos essenciais para o vídeo carregar e ter tamanho
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("width");
            sanitizer.AllowedAttributes.Add("height");
            sanitizer.AllowedAttributes.Add("frameborder");
            sanitizer.AllowedAttributes.Add("allow");
            sanitizer.AllowedAttributes.Add("allowfullscreen");
            sanitizer.AllowedAttributes.Add("controls");
            sanitizer.AllowedAttributes.Add("autoplay");
            // ---------------------------------------------------
            sanitizer.AllowedAttributes.Add("data-index");
            sanitizer.AllowedAttributes.Add("data-carousel");
            sanitizer.AllowedAttributes.Add("aria-label");
            sanitizer.AllowedAttributes.Add("aria-selected");
            sanitizer.AllowedAttributes.Add("aria-hidden");
            sanitizer.AllowedAttributes.Add("aria-live");
            sanitizer.AllowedAttributes.Add("role");
            sanitizer.AllowedAttributes.Add("tabindex");
            sanitizer.AllowedAttributes.Add("target");
            sanitizer.AllowedAttributes.Add("rel");
            // Permissões de Estrutura
            sanitizer.AllowedTags.Add("section");
            sanitizer.AllowedTags.Add("article");
            // ------------------------------------

            try 
            {
                var processedContent = await _productRenderer.ExpandProductBlocksAsync(post.Content ?? string.Empty);
                var rawHtml = _markdown.ToHtml(processedContent);
                
                // Agora o Sanitizer respeitará suas classes CSS
                cleanHtml = sanitizer.Sanitize(rawHtml);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(cacheKey, cleanHtml, cacheOptions);
            }
            catch (Exception)
            {
                cleanHtml = "<p>Erro ao carregar o conteúdo do artigo.</p>";
            }
        }

        var vm = new PostDetailViewModel
        {
            Post         = post,
            Settings     = settings,
            ContentHtml  = cleanHtml ?? string.Empty, 
            RelatedPosts = related,
            MostRead     = mostRead
        };
        
        return View(vm);
    }
}