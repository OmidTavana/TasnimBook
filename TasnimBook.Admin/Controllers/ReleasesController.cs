using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Models;
using TasnimBook.Admin.Services;
using TasnimBook.Data;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class ReleasesController(AdminApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new ReleasePageViewModel { Releases = await api.AppReleasesAsync(cancellationToken) };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(501L * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] ReleaseFormModel draft, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = string.Join(" | ", ModelState.Values.SelectMany(item => item.Errors).Select(item => item.ErrorMessage)) });
        try
        {
            await api.CreateAppReleaseAsync(draft, cancellationToken);
            return Ok(new { message = draft.Publish ? "نسخه با موفقیت منتشر شد." : "نسخه به‌صورت پیش‌نویس ذخیره شد." });
        }
        catch (AdminApiException exception)
        {
            return StatusCode(exception.StatusCode, new { message = exception.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        await api.PublishAppReleaseAsync(id, cancellationToken);
        TempData["Success"] = "نسخه منتشر شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool value, CancellationToken cancellationToken)
    {
        await api.SetAppReleaseActiveAsync(id, value, cancellationToken);
        TempData["Success"] = value ? "نسخه فعال شد." : "نسخه غیرفعال شد.";
        return RedirectToAction(nameof(Index));
    }
}
