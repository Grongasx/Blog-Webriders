using Microsoft.Extensions.Options;
using ThrottleBlog.Data;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using Microsoft.EntityFrameworkCore;
 
namespace ThrottleBlog.BackgroundServices;
 
/// <summary>
/// Worker em background que roda a cada X minutos,
/// busca itens Pending na NewsletterQueue e dispara os e-mails
/// via Resend para todos os subscribers ativos.
///
/// Fluxo por item da fila:
///   1. MarkProcessing  →  status = Processing
///   2. Para cada subscriber → SendAsync → grava NewsletterSendLog
///   3. MarkSent / MarkFailed
/// </summary>
public class NewsletterDispatchWorker : BackgroundService
{
    private readonly IServiceProvider          _services;
    private readonly ILogger<NewsletterDispatchWorker> _logger;
    private readonly NewsletterWorkerOptions   _opts;
 
    public NewsletterDispatchWorker(
        IServiceProvider services,
        ILogger<NewsletterDispatchWorker> logger,
        IOptions<NewsletterWorkerOptions> opts)
    {
        _services = services;
        _logger   = logger;
        _opts     = opts.Value;
    }
 
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NewsletterDispatchWorker iniciado. Intervalo: {Min} min.",
            _opts.IntervalMinutes);
 
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Erro no ciclo do NewsletterDispatchWorker.");
            }
 
            await Task.Delay(TimeSpan.FromMinutes(_opts.IntervalMinutes), stoppingToken);
        }
    }
 
    // ──────────────────────────────────────────────────────────
    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        // Scope por ciclo — evita DbContext de longa duração
        await using var scope = _services.CreateAsyncScope();
 
        var queue    = scope.ServiceProvider.GetRequiredService<INewsletterQueueService>();
        var email    = scope.ServiceProvider.GetRequiredService<IResendEmailService>();
        var template = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var db       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
 
        var pending = await queue.GetPendingAsync(ct);
 
        if (pending.Count == 0)
        {
            _logger.LogDebug("NewsletterQueue: nenhum item pendente.");
            return;
        }
 
        _logger.LogInformation("NewsletterQueue: {Count} item(s) para processar.", pending.Count);
 
        var blogSettings = await settings.GetAsync();
 
        // Subscribers ativos
        var subscribers = await db.NewsletterSubscribers
            .Where(s => s.IsActive)
            .ToListAsync(ct);
 
        if (subscribers.Count == 0)
        {
            _logger.LogWarning("Nenhum subscriber ativo. Pulando fila.");
            return;
        }
 
        foreach (var item in pending)
        {
            await ProcessItemAsync(item, subscribers, queue, email, template, blogSettings, db, ct);
        }
    }
 
    // ──────────────────────────────────────────────────────────
    private async Task ProcessItemAsync(
        NewsletterQueue          item,
        List<NewsletterSubscriber> subscribers,
        INewsletterQueueService  queue,
        IResendEmailService      email,
        IEmailTemplateService    template,
        BlogSettings             settings,
        AppDbContext             db,
        CancellationToken        ct)
    {
        await queue.MarkProcessingAsync(item.Id, ct);
        _logger.LogInformation("Processando fila #{Id} — Post: {Title}", item.Id, item.Post.Title);

        int sent   = 0;
        int failed = 0;

        // Busca IDs já enviados para evitar duplicidade
        var alreadySentList = await db.NewsletterSendLog
            .Where(l => l.NewsletterQueueId == item.Id && l.Success)
            .Select(l => l.SubscriberId)
            .ToListAsync(ct);
        
        var alreadySent = alreadySentList.ToHashSet();

        // Definição do Assunto (Subject)
        var subject = $"[Webriders] {item.Post.Title}";

        foreach (var sub in subscribers)
        {
            if (alreadySent.Contains(sub.Id)) continue;
            if (ct.IsCancellationRequested)   break;

            var html = template.BuildNewPostEmail(item.Post, sub, settings);

            // Corrigindo a chamada do SendAsync e a desconstrução (Tuple)
            // Corrigindo para os nomes exatos que o compilador identificou
            var result = await email.SendAsync(sub.Email, sub.Name, subject, html, ct);
            bool ok = result.Success;
            string? msgId = result.MessageId;
            string? err = result.Error; // <--- O erro estava aqui, mude de ErrorMessage para Error

            db.NewsletterSendLog.Add(new NewsletterSendLog
            {
                NewsletterQueueId = item.Id,
                SubscriberId      = sub.Id,
                Success           = ok,
                ResendMessageId   = msgId,
                ErrorMessage      = err, // Aqui você mapeia a variável 'err' para a coluna do banco
                SentAt            = DateTime.UtcNow
            });

            if (ok) sent++;
            else
            {
                failed++;
                _logger.LogWarning("Falha ao enviar para {Email}: {Err}", sub.Email, err);
            }

            if ((sent + failed) % 50 == 0)
                await db.SaveChangesAsync(ct);

            await Task.Delay(_opts.DelayBetweenEmailsMs, ct);
        }

        await db.SaveChangesAsync(ct);
        await queue.MarkSentAsync(item.Id, sent, failed, ct);

        _logger.LogInformation("Fila #{Id} concluída — Enviados: {Sent} | Falhas: {Failed}", item.Id, sent, failed);
    }
}
 
// ──────────────────────────────────────────────────────────────
//  Opções configuráveis via appsettings.json
// ──────────────────────────────────────────────────────────────
public class NewsletterWorkerOptions
{
    public const string Section = "NewsletterWorker";
 
    /// <summary>Intervalo em minutos entre cada ciclo do worker (padrão: 5).</summary>
    public int IntervalMinutes { get; set; } = 5;
 
    /// <summary>Delay em ms entre cada e-mail enviado (padrão: 520ms = ~2/s).</summary>
    public int DelayBetweenEmailsMs { get; set; } = 520;
}