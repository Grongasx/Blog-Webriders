// ============================================================================
// 1. NAMESPACES E DEPENDÊNCIAS
// ============================================================================
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThrottleBlog.Models;
using ThrottleBlog.ViewModels;

namespace ThrottleBlog.Controllers;

/// <summary>
/// Controlador responsável pelo ecossistema de autenticação do ThrottleBlog.
/// Gerencia o ciclo de vida das sessões administrativas utilizando ASP.NET Core Identity.
/// </summary>
public class AuthController : Controller
{
    // ============================================================================
    // 2. PROPRIEDADES PRIVADAS (INJEÇÃO DE DEPENDÊNCIA)
    // ============================================================================
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser>   _users;

    // ============================================================================
    // 3. CONSTRUTOR
    // ============================================================================
    public AuthController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser>   users)
    {
        _signIn = signIn;
        _users  = users;
    }

    #region FLUXO DE AUTENTICAÇÃO (LOGIN)

    /// <summary>
    /// GET /admin/login
    /// Renderiza a tela de login. Se o usuário já possuir uma sessão ativa, é redirecionado ao Dashboard.
    /// </summary>
    [HttpGet("/admin/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToAction("Index", "Admin");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// POST /admin/login
    /// Valida as credenciais fornecidas e inicia a sessão criptográfica do usuário.
    /// Implementa proteção contra força bruta bloqueando a conta temporariamente após falhas consecutivas.
    /// </summary>
    [HttpPost("/admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Executa a tentativa de login via Cookie Authentication persistente ou de sessão
        var result = await _signIn.PasswordSignInAsync(
            vm.UserName, 
            vm.Password,
            isPersistent: vm.RememberMe,
            lockoutOnFailure: true 
        );

        if (result.Succeeded)
        {
            // Sanitiza o redirecionamento para mitigar vulnerabilidades de Open Redirect
            var local = Url.IsLocalUrl(vm.ReturnUrl);
            return local ? Redirect(vm.ReturnUrl!) : RedirectToAction("Index", "Admin");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Conta bloqueada por excesso de tentativas. Tente mais tarde.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha incorretos.");
        }

        return View(vm);
    }

    #endregion

    #region ENCERRAMENTO DE SESSÃO (LOGOUT)

    /// <summary>
    /// POST /admin/logout
    /// Invalida o cookie de autenticação atual e encerra completamente a sessão do usuário.
    /// </summary>
    [HttpPost("/admin/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region SEGURANÇA E ERROS

    /// <summary>
    /// GET /admin/acesso-negado
    /// Rota de fallback interceptada pelo Identity quando o usuário está autenticado mas não possui a Role 'Admin'.
    /// </summary>
    [HttpGet("/admin/acesso-negado")]
    public IActionResult AccessDenied() => View();

    #endregion
}