using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class HomeController : Controller
{
    private readonly AdminApiClient _apiClient;

    public HomeController(AdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var dashboard = await _apiClient.DashboardAsync(cancellationToken);
            return View(dashboard);
        }
        catch (Exception exception)
        {
            ViewBag.Error = exception.Message;
            return View(new DashboardDto());
        }
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
