using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.SuperAdmin)]
public sealed class AdministratorsController(AdminApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token) => View(await api.AdministratorsAsync(token));
    public async Task<IActionResult> Pending(CancellationToken token) => View(await api.PendingAsync(token));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, bool approve, string role, string? note, CancellationToken token)
    { await api.ReviewAsync(id, new AdminReviewRequest { Approve = approve, Role = role, Note = note }, token); return RedirectToAction(nameof(Pending)); }
}
