using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController, ApiKeyAuth, Authorize(Roles = UserRoles.Administrators)]
[Route("api/admin-app-releases")]
public sealed class AdminAppReleasesController(DbContextAnti db, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaximumPackageSize = 500L * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<AdminAppReleaseListDto>> List(CancellationToken cancellationToken)
    {
        var releases = await db.AppVersions.AsNoTracking().OrderByDescending(item => item.ReleaseDate).ToListAsync(cancellationToken);
        return Ok(new AdminAppReleaseListDto
        {
            LatestAndroid = ToDto(AppVersionOrdering.Latest(releases.Where(item => item.PlatformId == (int)Platforms.Android))),
            LatestWindows = ToDto(AppVersionOrdering.Latest(releases.Where(item => item.PlatformId == (int)Platforms.Windows))),
            History = releases.Select(ToDto).Where(item => item is not null).Cast<AppReleaseDto>().ToList()
        });
    }

    [HttpPost]
    [RequestSizeLimit(MaximumPackageSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumPackageSize + 1024 * 1024)]
    public async Task<ActionResult<AppReleaseDto>> Create(
        [FromForm] IFormFile file,
        [FromForm] int platformId,
        [FromForm] string version,
        [FromForm] int build,
        [FromForm] string releaseNotes,
        [FromForm] bool publish,
        [FromForm] bool isMandatory,
        CancellationToken cancellationToken)
    {
        if (platformId is not ((int)Platforms.Android) and not ((int)Platforms.Windows)) return BadRequest(new { message = "پلتفرم معتبر نیست." });
        if (!Version.TryParse(version.Trim().TrimStart('v', 'V'), out _)) return BadRequest(new { message = "شماره نسخه باید مانند 1.2.0 باشد." });
        if (build <= 0) return BadRequest(new { message = "شماره Build باید بزرگ‌تر از صفر باشد." });
        if (string.IsNullOrWhiteSpace(releaseNotes) || releaseNotes.Length > 4000) return BadRequest(new { message = "توضیحات تغییرات الزامی و حداکثر ۴۰۰۰ نویسه است." });
        if (file.Length is <= 0 or > MaximumPackageSize) return BadRequest(new { message = "فایل خالی یا بزرگ‌تر از ۵۰۰ مگابایت قابل قبول نیست." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = platformId == (int)Platforms.Android
            ? new[] { ".apk" }
            : new[] { ".exe", ".msix", ".msixbundle", ".appx", ".appxbundle" };
        if (!allowed.Contains(extension) || !await HasValidSignatureAsync(file, extension, cancellationToken))
            return BadRequest(new { message = "نوع یا محتوای فایل نصب با پلتفرم انتخاب‌شده سازگار نیست." });

        var duplicate = await db.AppVersions.AnyAsync(item => item.PlatformId == platformId && item.VersionName == version.Trim() && item.VersionNumber == build, cancellationToken);
        if (duplicate) return Conflict(new { message = "این نسخه و Build قبلاً برای پلتفرم انتخاب‌شده ثبت شده است." });

        var platformFolder = platformId == (int)Platforms.Android ? "android" : "windows";
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "WWWROOT");
        var directory = Path.Combine(root, "Upload", "releases", platformFolder);
        Directory.CreateDirectory(directory);
        var storedFileName = $"tasnimbook-{version.Trim()}-{build}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(directory, storedFileName);

        try
        {
            await using (var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, true))
                await file.CopyToAsync(destination, cancellationToken);
            string hash;
            await using (var hashStream = System.IO.File.OpenRead(destinationPath))
                hash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken)).ToLowerInvariant();
            var release = new AppVersion
            {
                PlatformId = platformId,
                VersionName = version.Trim().TrimStart('v', 'V'),
                VersionNumber = build,
                ReleaseNotes = releaseNotes.Trim(),
                ReleaseDate = publish ? DateTime.UtcNow : DateTime.UtcNow,
                IsPublished = publish,
                IsActive = true,
                IsMandatory = isMandatory,
                FileSize = file.Length,
                Sha256 = hash,
                StoredFileName = storedFileName,
                ContentType = ContentType(extension),
                Url = "pending"
            };
            db.AppVersions.Add(release);
            await db.SaveChangesAsync(cancellationToken);
            release.Url = $"/api/app-releases/{release.Id}/download";
            await db.SaveChangesAsync(cancellationToken);
            return Created($"/api/app-releases/{release.Id}", ToDto(release));
        }
        catch
        {
            if (System.IO.File.Exists(destinationPath)) System.IO.File.Delete(destinationPath);
            throw;
        }
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var release = await db.AppVersions.FindAsync([id], cancellationToken);
        if (release is null) return NotFound();
        if (string.IsNullOrWhiteSpace(release.StoredFileName)) return Conflict(new { message = "نسخه بدون فایل معتبر قابل انتشار نیست." });
        release.IsPublished = true;
        release.IsActive = true;
        release.ReleaseDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool value, CancellationToken cancellationToken)
    {
        var release = await db.AppVersions.FindAsync([id], cancellationToken);
        if (release is null) return NotFound();
        release.IsActive = value;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private AppReleaseDto? ToDto(AppVersion? release)
    {
        if (release is null) return null;
        return new AppReleaseDto
        {
            Id = release.Id, PlatformId = release.PlatformId, Platform = ((Platforms)release.PlatformId).ToString().ToLowerInvariant(),
            Version = release.VersionName, Build = release.VersionNumber, ReleaseNotes = release.ReleaseNotes,
            DownloadUrl = $"/api/app-releases/{release.Id}/download", FileSize = release.FileSize, Sha256 = release.Sha256,
            ContentType = release.ContentType, IsMandatory = release.IsMandatory, IsPublished = release.IsPublished,
            IsActive = release.IsActive, PublishedAt = release.ReleaseDate
        };
    }

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await using var stream = file.OpenReadStream();
        if (await stream.ReadAsync(header, cancellationToken) < header.Length) return false;
        return extension == ".exe"
            ? header[0] == 0x4D && header[1] == 0x5A
            : header[0] == 0x50 && header[1] == 0x4B && header[2] is 0x03 or 0x05 or 0x07 && header[3] is 0x04 or 0x06 or 0x08;
    }

    private static string ContentType(string extension) => extension switch
    {
        ".apk" => "application/vnd.android.package-archive",
        ".msix" or ".msixbundle" or ".appx" or ".appxbundle" => "application/vnd.ms-appx",
        ".exe" => "application/vnd.microsoft.portable-executable",
        _ => "application/octet-stream"
    };
}
