using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.SuperAdmin)]
public sealed class AdministratorsController : Controller
{
    private readonly AdminApiClient _apiClient;

    public AdministratorsController(AdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var administrators = await _apiClient.AdministratorsAsync(cancellationToken);

        return View(administrators);
    }

    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        var pendingAdministrators = await _apiClient.PendingAsync(cancellationToken);

        return View(pendingAdministrators);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        int id,
        bool approve,
        string role,
        string? note,
        CancellationToken cancellationToken)
    {
        var reviewRequest = new AdminReviewRequest
        {
            Approve = approve,
            Role = role,
            Note = note
        };

        await _apiClient.ReviewAsync(id, reviewRequest, cancellationToken);

        return RedirectToAction(nameof(Pending));
    }
}
