using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Controllers;

public sealed class AccountController(AdminApiClient api) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Login() => View(new LoginAccountRequest());
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginAccountRequest request, CancellationToken token)
    {
        if (!ModelState.IsValid) return View(request);
        try
        {
            var result = await api.LoginAsync(request, token);
            if (result.User.AdminStatus == AdminApprovalStatuses.PendingApproval) return View("Pending");
            if (result.User.AdminStatus == AdminApprovalStatuses.Rejected) return View("Rejected");
            if (result.User.Role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin)) return View("Unauthorized");
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()), new Claim(ClaimTypes.Name, result.User.DisplayName), new Claim(ClaimTypes.Email, result.User.Email), new Claim(ClaimTypes.Role, result.User.Role), new Claim("access_token", result.AccessToken) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(request); }
    }
    [AllowAnonymous, HttpGet] public IActionResult Register() => View(new AdminRegisterRequest());
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AdminRegisterRequest request, CancellationToken token)
    { if (!ModelState.IsValid) return View(request); try { await api.RegisterAsync(request, token); return View("Pending"); } catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(request); } }
    [Authorize, HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous] public IActionResult AccessDenied() => View("Unauthorized");
}
