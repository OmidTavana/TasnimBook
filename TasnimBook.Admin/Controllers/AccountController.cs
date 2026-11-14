using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Controllers;

public sealed class AccountController : Controller
{
    private readonly AdminApiClient _apiClient;

    public AccountController(AdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginAccountRequest());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var result = await _apiClient.LoginAsync(request, cancellationToken);

            if (result.User.AdminStatus == AdminApprovalStatuses.PendingApproval)
            {
                return View("Pending");
            }

            if (result.User.AdminStatus == AdminApprovalStatuses.Rejected)
            {
                return View("Rejected");
            }

            if (result.User.Role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin))
            {
                return View("Unauthorized");
            }

            var principal = CreatePrincipal(result);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(request);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new AdminRegisterRequest());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        AdminRegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _apiClient.RegisterAsync(request, cancellationToken);
            return View("Pending");
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(request);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View("Unauthorized");
    }

    private static ClaimsPrincipal CreatePrincipal(AccountAuthResult result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new(ClaimTypes.Name, result.User.DisplayName),
            new(ClaimTypes.Email, result.User.Email),
            new(ClaimTypes.Role, result.User.Role),
            new("access_token", result.AccessToken)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
