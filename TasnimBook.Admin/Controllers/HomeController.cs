using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class HomeController(AdminApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    { try { return View(await api.DashboardAsync(token)); } catch (Exception ex) { ViewBag.Error = ex.Message; return View(new TasnimBook.Data.Models.DTO.DashboardDto()); } }
    [AllowAnonymous] public IActionResult Error() => View();
}
