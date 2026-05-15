using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Models;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Autenticação do administrador via ASP.NET Core Identity.
/// </summary>
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser>   _users;

    public AuthController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser>   users)
    {
        _signIn = signIn;
        _users  = users;
    }

    // GET /auth/login
    [HttpGet("/admin/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToAction("Index", "Admin");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST /admin/login
    [HttpPost("/admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signIn.PasswordSignInAsync(
            vm.UserName, vm.Password,
            isPersistent: vm.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var local = Url.IsLocalUrl(vm.ReturnUrl);
            return local ? Redirect(vm.ReturnUrl!) : RedirectToAction("Index", "Admin");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Conta bloqueada por excesso de tentativas. Tente mais tarde.");
        }
        else
        {
            ModelState.AddModelError("", "Usuário ou senha incorretos.");
        }
        return View(vm);
    }

    // POST /admin/logout
    [HttpPost("/admin/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // GET /admin/acesso-negado
    [HttpGet("/admin/acesso-negado")]
    public IActionResult AccessDenied() => View();
}
