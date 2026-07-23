using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Models;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador responsável pelas campanhas de captura e submissão de assinaturas da Newsletter.
/// </summary>
[Route("newsletter")]
public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletter;
    private readonly ISettingsService   _settings;

    public NewsletterController(INewsletterService newsletter, ISettingsService settings)
    {
        _newsletter = newsletter;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> NewsletterPage()
    {
        var settings = await _settings.GetAsync();
        var counts   = await _newsletter.CountByTierAsync();
        
        return View("Newsletter", new NewsletterPageViewModel
        {
            Settings     = settings,
            CommonCount  = counts.Common,
            PremiumCount = counts.Premium
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(NewsletterFormViewModel vm, string? returnTo = null)
    {
        var isAjax   = IsAjaxRequest();
        var redirect = returnTo == "Index" ? "Index" : nameof(NewsletterPage);
        
        if (!ModelState.IsValid)
            return NewsletterFeedback(isAjax, false, "Preencha nome e e-mail válidos.", redirect);

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, NewsletterTier.Common);
        return NewsletterFeedback(isAjax, result.Success, result.Message, redirect);
    }

    [HttpPost("premium")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubscribePremium(PremiumNewsletterFormViewModel vm)
    {
        var isAjax = IsAjaxRequest();
        
        if (!ModelState.IsValid)
            return NewsletterFeedback(isAjax, false, "Preencha todos os campos corretamente.", nameof(NewsletterPage));

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, NewsletterTier.Premium, vm.Phone);
        return NewsletterFeedback(isAjax, result.Success, result.Message, nameof(NewsletterPage));
    }

    #region MÉTODOS AUXILIARES

    private bool IsAjaxRequest()
        => string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

    private IActionResult NewsletterFeedback(bool isAjax, bool success, string message, string redirectAction)
    {
        if (isAjax) return Json(new { success, message });
            
        TempData[success ? "NewsletterSuccess" : "NewsletterError"] = message;
        return redirectAction == "Index" 
            ? RedirectToAction("Index", "Home") 
            : RedirectToAction(redirectAction);
    }

    #endregion
}