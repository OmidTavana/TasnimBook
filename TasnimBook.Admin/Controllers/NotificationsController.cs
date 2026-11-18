using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Models;
using TasnimBook.Admin.Services;
using TasnimBook.Data;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.SuperAdmin)]
public sealed class NotificationsController(AdminApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new NotificationPageModel
        {
            Items = await apiClient.NotificationsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NotificationPageModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Items = await apiClient.NotificationsAsync(cancellationToken);
            return View(nameof(Index), model);
        }

        try
        {
            await apiClient.CreateNotificationAsync(model.Draft, cancellationToken);
            TempData["Success"] = "اعلان جدید ثبت شد.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Items = await apiClient.NotificationsAsync(cancellationToken);
            return View(nameof(Index), model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await apiClient.DeleteNotificationAsync(id, cancellationToken);
        TempData["Success"] = "اعلان حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}
