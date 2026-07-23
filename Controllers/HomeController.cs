using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Services;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador responsável pelas páginas institucionais e pontos de entrada globais.
/// </summary>
public class HomeController : Controller
{
    private readonly IPostService     _posts;
    private readonly ICategoryService _categories;
    private readonly ISettingsService   _settings;

    public HomeController(IPostService posts, ICategoryService categories, ISettingsService settings)
    {
        _posts      = posts;
        _categories = categories;
        _settings   = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            Settings      = await _settings.GetAsync(),
            FeaturedPosts = await _posts.GetFeaturedAsync(3),
            LatestPosts   = await _posts.GetLatestAsync(6),
            MostRead      = await _posts.GetMostReadAsync(3),
            Categories    = await _categories.GetAllAsync(),
        };

        if (!vm.FeaturedPosts.Any())
            vm.FeaturedPosts = vm.LatestPosts.Take(3).ToList();

        return View(vm);
    }

    [Route("erro/{code:int}")]
    public IActionResult Error(int code) => View("Error", code);
}