using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Models.DTO;

namespace webapicore.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/app-releases")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AppReleasesController(DbContextAnti db, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("latest")]
    public async Task<ActionResult<AppReleaseDto>> Latest(string platform, CancellationToken cancellationToken)
    {
        if (!TryPlatform(platform, out var platformId)) return BadRequest(new { message = "پلتفرم معتبر نیست." });
        var release = await FindLatestAsync(platformId, cancellationToken);
        return release is null ? NotFound(new { message = "نسخه منتشرشده‌ای برای این پلتفرم وجود ندارد." }) : Ok(ToDto(release));
    }

    [HttpGet("check-update")]
    public async Task<ActionResult<AppUpdateCheckDto>> CheckUpdate(
        string platform,
        string? currentVersion,
        int build = 0,
        CancellationToken cancellationToken = default)
    {
        if (!TryPlatform(platform, out var platformId)) return BadRequest(new { message = "پلتفرم معتبر نیست." });
        var release = await FindLatestAsync(platformId, cancellationToken);
        if (release is null) return Ok(new AppUpdateCheckDto { UpdateAvailable = false });

        var dto = ToDto(release);
        return Ok(new AppUpdateCheckDto
        {
            UpdateAvailable = AppVersionOrdering.IsNewer(release, currentVersion, build),
            LatestVersion = dto.Version,
            LatestBuild = dto.Build,
            ReleaseNotes = dto.ReleaseNotes,
            DownloadUrl = dto.DownloadUrl,
            FileSize = dto.FileSize,
            Sha256 = dto.Sha256,
            IsMandatory = dto.IsMandatory,
            PublishedAt = dto.PublishedAt
        });
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var release = await db.AppVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.IsPublished && item.IsActive, cancellationToken);
        if (release is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(release.StoredFileName))
        {
            var platformFolder = release.PlatformId == (int)Platforms.Android ? "android" : "windows";
            var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "WWWROOT");
            var directory = Path.GetFullPath(Path.Combine(root, "Upload", "releases", platformFolder));
            var filePath = Path.GetFullPath(Path.Combine(directory, Path.GetFileName(release.StoredFileName)));
            if (!filePath.StartsWith(directory, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(filePath)) return NotFound();
            return PhysicalFile(filePath, release.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
        }

        if (Uri.TryCreate(release.Url, UriKind.Absolute, out var absolute)) return Redirect(absolute.ToString());
        if (!string.IsNullOrWhiteSpace(release.Url) && release.Url.StartsWith('/') && !release.Url.Contains("app-releases", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(release.Url);
        return NotFound();
    }

    private async Task<AppVersion?> FindLatestAsync(int platformId, CancellationToken cancellationToken)
    {
        var candidates = await db.AppVersions.AsNoTracking()
            .Where(item => item.PlatformId == platformId && item.IsPublished && item.IsActive)
            .ToListAsync(cancellationToken);
        return AppVersionOrdering.Latest(candidates);
    }

    private AppReleaseDto ToDto(AppVersion release) => new()
    {
        Id = release.Id,
        PlatformId = release.PlatformId,
        Platform = ((Platforms)release.PlatformId).ToString().ToLowerInvariant(),
        Version = release.VersionName,
        Build = release.VersionNumber,
        ReleaseNotes = release.ReleaseNotes,
        DownloadUrl = Url.ActionLink(nameof(Download), values: new { id = release.Id }) ?? release.Url,
        FileSize = release.FileSize,
        Sha256 = release.Sha256,
        ContentType = release.ContentType,
        IsMandatory = release.IsMandatory,
        IsPublished = release.IsPublished,
        IsActive = release.IsActive,
        PublishedAt = release.ReleaseDate
    };

    private static bool TryPlatform(string value, out int platformId)
    {
        platformId = value.Trim().ToLowerInvariant() switch
        {
            "android" => (int)Platforms.Android,
            "windows" => (int)Platforms.Windows,
            _ => 0
        };
        return platformId != 0;
    }
}
