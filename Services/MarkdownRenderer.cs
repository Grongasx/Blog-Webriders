using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace ThrottleBlog.Services;

public interface IMarkdownRenderer
{
    string ToHtml(string? markdown);
}

public class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    // 1. Regex para blocos de produtos
    private static readonly Regex ProductBlockRegex = new(
        @":::produto\s*\n([\s\S]*?)\n:::\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 2. NOVO: Regex para blocos de vídeo do Painel Admin (:::video URL :::)
    private static readonly Regex VideoBlockRegex = new(
        @":::video\s+(https?://[^\s]+)\s+:::",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 3. Links soltos perdidos no texto (Fallback)
    private static readonly Regex YoutubeLineRegex = new(
        @"^\s*(?:https?://)?(?:www\.)?(?:m\.)?(?:youtube\.com/watch\?[^""]*v=|youtu\.be/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})[^\r\n]*",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // 4. Tags <a> do HTML (Fallback para posts antigos do BD)
    private static readonly Regex YoutubeLinkRegex = new(
        @"<a\s+[^>]*href=[""']https?://(?:www\.)?(?:m\.)?(?:youtube\.com/watch\?[^""']*v=|youtu\.be/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})[^""']*[""'][^>]*>[\s\S]*?</a>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        // Processa produtos
        var processed = ProductBlockRegex.Replace(markdown, m => BuildProductCard(m.Groups[1].Value));
        
        // NOVO: Processa os blocos :::video URL ::: criados no PostForm.cshtml
        processed = VideoBlockRegex.Replace(processed, m => BuildVideoEmbedFromUrl(m.Groups[1].Value));
        
        // Trata links soltos 
        processed = YoutubeLineRegex.Replace(processed, m => BuildVideoEmbed(m.Groups[1].Value));
        
        // Converte o Markdown restante para HTML
        var html = Markdown.ToHtml(processed, Pipeline);
        
        // Trata tags HTML antigas
        html = YoutubeLinkRegex.Replace(html, m => BuildVideoEmbed(m.Groups[1].Value));
        
        return html;
    }

    private static string BuildProductCard(string block)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line[..idx].Trim().ToLowerInvariant();
            var val = line[(idx + 1)..].Trim();
            fields[key] = val;
        }

        if (!fields.TryGetValue("url", out var url) || string.IsNullOrWhiteSpace(url))
            return string.Empty;

        fields.TryGetValue("title", out var title);
        fields.TryGetValue("price", out var price);
        fields.TryGetValue("image", out var image);

        title ??= "Ver produto";
        var safeUrl   = WebUtility.HtmlEncode(url);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safePrice = !string.IsNullOrWhiteSpace(price) ? WebUtility.HtmlEncode(price) : null;
        var imgHtml   = !string.IsNullOrWhiteSpace(image)
            ? $"<img src=\"{WebUtility.HtmlEncode(image)}\" alt=\"{safeTitle}\">"
            : "";

        var priceHtml = safePrice is not null
            ? $"<div class=\"product-card-price\">{safePrice}</div>"
            : "";

        return $"""
            <a class="product-card" href="{safeUrl}" target="_blank" rel="noopener noreferrer">
              {imgHtml}
              <div class="product-card-body">
                <div class="product-card-title">{safeTitle}</div>
                {priceHtml}
                <span class="product-card-cta">Ver produto →</span>
              </div>
            </a>
            """;
    }

    // NOVO: Lê a URL extraída do :::video e suporta YouTube e Vimeo (igual ao seu JS do Admin)
    private static string BuildVideoEmbedFromUrl(string url)
    {
        var ytMatch = Regex.Match(url, @"(?:youtube\.com/watch\?[^""]*v=|youtu\.be/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase);
        if (ytMatch.Success) 
            return BuildVideoEmbed(ytMatch.Groups[1].Value);

        var vimeoMatch = Regex.Match(url, @"vimeo\.com/(\d+)", RegexOptions.IgnoreCase);
        if (vimeoMatch.Success)
            return $"<div class=\"video-embed\"><iframe src=\"https://player.vimeo.com/video/{vimeoMatch.Groups[1].Value}\" title=\"Vimeo video\" allowfullscreen loading=\"lazy\"></iframe></div>";

        return string.Empty;
    }

    private static string BuildVideoEmbed(string videoId)
        => $"<div class=\"video-embed\"><iframe src=\"https://www.youtube.com/embed/{videoId}\" title=\"YouTube video\" allowfullscreen loading=\"lazy\"></iframe></div>";
}