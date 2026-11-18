using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController]
[ApiKeyAuth]
[Authorize(Roles = UserRoles.SuperAdmin)]
[Route("api/[controller]")]
public sealed class AdminNotificationsController(DbContextAnti db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppNotification>>> List(CancellationToken cancellationToken) =>
        Ok(await db.AppNotifications.AsNoTracking()
            .OrderByDescending(item => item.PublishAtUtc)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(AppNotification input, CancellationToken cancellationToken)
    {
        var validationResult = Validate(input);
        if (validationResult is not null) return validationResult;

        input.Id = 0;
        input.Title = input.Title.Trim();
        input.Message = input.Message.Trim();
        input.ActionUrl = string.IsNullOrWhiteSpace(input.ActionUrl) ? null : input.ActionUrl.Trim();
        input.VersionName = string.IsNullOrWhiteSpace(input.VersionName) ? null : input.VersionName.Trim();
        input.CreatedAtUtc = DateTime.UtcNow;
        input.PublishAtUtc = NormalizeUtc(input.PublishAtUtc);
        input.ExpiresAtUtc = input.ExpiresAtUtc.HasValue ? NormalizeUtc(input.ExpiresAtUtc.Value) : null;
        db.AppNotifications.Add(input);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { input.Id }, input);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AppNotification input, CancellationToken cancellationToken)
    {
        var validationResult = Validate(input);
        if (validationResult is not null) return validationResult;

        var notification = await db.AppNotifications.FindAsync([id], cancellationToken);
        if (notification is null) return NotFound();
        notification.Title = input.Title.Trim();
        notification.Message = input.Message.Trim();
        notification.NotificationType = input.NotificationType;
        notification.ActionUrl = string.IsNullOrWhiteSpace(input.ActionUrl) ? null : input.ActionUrl.Trim();
        notification.VersionName = string.IsNullOrWhiteSpace(input.VersionName) ? null : input.VersionName.Trim();
        notification.VersionNumber = input.VersionNumber;
        notification.PlatformId = input.PlatformId;
        notification.PublishAtUtc = NormalizeUtc(input.PublishAtUtc);
        notification.ExpiresAtUtc = input.ExpiresAtUtc.HasValue ? NormalizeUtc(input.ExpiresAtUtc.Value) : null;
        notification.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var notification = await db.AppNotifications.FindAsync([id], cancellationToken);
        if (notification is null) return NotFound();
        db.AppNotifications.Remove(notification);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private BadRequestObjectResult? Validate(AppNotification input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Message))
            return BadRequest(new { message = "عنوان و متن اعلان الزامی است." });
        if (input.NotificationType is not (AppNotificationTypes.General or AppNotificationTypes.AppUpdate))
            return BadRequest(new { message = "نوع اعلان معتبر نیست." });
        if (input.NotificationType == AppNotificationTypes.AppUpdate && input.VersionNumber is null)
            return BadRequest(new { message = "برای اعلان آپدیت، شماره ساخت نسخه الزامی است." });
        if (input.ExpiresAtUtc.HasValue && input.ExpiresAtUtc <= input.PublishAtUtc)
            return BadRequest(new { message = "زمان پایان باید بعد از زمان انتشار باشد." });
        return null;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
