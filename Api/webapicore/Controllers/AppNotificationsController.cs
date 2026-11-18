using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController]
[ApiKeyAuth]
[Route("api/[controller]")]
public sealed class AppNotificationsController(DbContextAnti db) : ControllerBase
{
    [HttpGet("Active")]
    public async Task<ActionResult<IReadOnlyList<AppNotification>>> Active(
        int platformId,
        int currentVersion,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var notifications = await db.AppNotifications.AsNoTracking()
            .Where(item => item.IsActive &&
                           item.PublishAtUtc <= now &&
                           (!item.ExpiresAtUtc.HasValue || item.ExpiresAtUtc > now) &&
                           (!item.PlatformId.HasValue || item.PlatformId == platformId) &&
                           (item.NotificationType != AppNotificationTypes.AppUpdate ||
                            !item.VersionNumber.HasValue || item.VersionNumber > currentVersion))
            .OrderBy(item => item.PublishAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }
}
