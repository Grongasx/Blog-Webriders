using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador público de Newsletter (Landing page, Assinatura Gratuita, Checkout Premium e Confirmação de Sucesso).
/// </summary>
public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletter;
    private readonly ISettingsService _settings;

    public NewsletterController(INewsletterService newsletter, ISettingsService settings)
    {
        _newsletter = newsletter;
        _settings = settings;
    }

    // GET /newsletter
    [HttpGet("newsletter")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Settings = await _settings.GetAsync();
        return View();
    }

    // POST /newsletter/inscrever
    [HttpPost("newsletter/inscrever")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InscreverGratis(NewsletterFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["NewsletterError"] = "Preencha um nome e e-mail válidos.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, ThrottleBlog.Models.NewsletterTier.Common);
        if (result.Success) // Se a propriedade for diferente no seu serviço (ex: IsSuccess), ajuste aqui
        {
            return RedirectToAction(nameof(Sucesso), new { email = vm.Email, plan = "Gratuito" });
        }

        TempData["NewsletterError"] = "Este e-mail já está cadastrado nesta modalidade.";
        return RedirectToAction(nameof(Index));
    }

    // GET /newsletter/checkout
    [HttpGet("newsletter/checkout")]
    public async Task<IActionResult> Checkout()
    {
        ViewBag.Settings = await _settings.GetAsync();
        return View(new NewsletterFormViewModel());
    }

    // POST /newsletter/checkout
    [HttpPost("newsletter/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPremium(NewsletterFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Settings = await _settings.GetAsync();
            return View("Checkout", vm);
        }

        var result = await _newsletter.SubscribeAsync(vm.Name, vm.Email, ThrottleBlog.Models.NewsletterTier.Premium);
        if (result.Success)
        {
            return RedirectToAction(nameof(Sucesso), new { email = vm.Email, plan = "Premium" });
        }

        TempData["NewsletterError"] = "Este e-mail já possui uma assinatura Premium ativa.";
        return RedirectToAction(nameof(Index));
    }

    // GET /newsletter/sucesso
    [HttpGet("newsletter/sucesso")]
    public async Task<IActionResult> Sucesso(string email, string plan)
    {
        ViewBag.Settings = await _settings.GetAsync();
        ViewBag.Email = email;
        ViewBag.Plan = plan;
        return View();
    }
}
