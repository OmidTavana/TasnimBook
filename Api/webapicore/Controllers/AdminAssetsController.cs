using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController]
[ApiKeyAuth]
[Authorize(Roles = UserRoles.Administrators)]
[Route("api/[controller]")]
public sealed class AdminAssetsController : ControllerBase
{
    private const long MaximumCoverSize = 8 * 1024 * 1024;
    private const long MaximumDocumentSize = 100 * 1024 * 1024;
    private const long MaximumAudioSize = 300 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, UploadPolicy> Policies =
        new Dictionary<string, UploadPolicy>(StringComparer.OrdinalIgnoreCase)
        {
            ["cover"] = new(
                "covers",
                MaximumCoverSize,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [".jpg"] = "image/jpeg",
                    [".jpeg"] = "image/jpeg",
                    [".png"] = "image/png",
                    [".webp"] = "image/webp"
                }),
            ["document"] = new(
                "documents",
                MaximumDocumentSize,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [".pdf"] = "application/pdf"
                }),
            ["audio"] = new(
                "audio",
                MaximumAudioSize,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [".mp3"] = "audio/mpeg"
                })
        };

    private readonly IWebHostEnvironment _environment;

    public AdminAssetsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("Upload")]
    [RequestSizeLimit(MaximumAudioSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumAudioSize + 1024 * 1024)]
    public async Task<ActionResult<AdminAssetUploadDto>> Upload(
        [FromForm] IFormFile file,
        [FromQuery] string kind,
        [FromQuery] string? bookTitle,
        CancellationToken cancellationToken)
    {
        if (!Policies.TryGetValue(kind, out var policy))
        {
            return BadRequest(new { message = "نوع فایل انتخاب‌شده معتبر نیست." });
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "فایل خالی قابل آپلود نیست." });
        }

        if (file.Length > policy.MaximumSize)
        {
            var maximumSizeInMb = policy.MaximumSize / 1024 / 1024;
            return BadRequest(new { message = $"حجم فایل نباید بیشتر از {maximumSizeInMb} مگابایت باشد." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!policy.AllowedContentTypes.TryGetValue(extension, out var expectedContentType) ||
            !HasExpectedContentType(file.ContentType, expectedContentType))
        {
            return BadRequest(new { message = "فرمت فایل انتخاب‌شده پشتیبانی نمی‌شود." });
        }

        if (!await HasValidSignatureAsync(file, extension, cancellationToken))
        {
            return BadRequest(new { message = "محتوای فایل با فرمت آن مطابقت ندارد." });
        }

        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "WWWROOT");
        var uploadDirectory = Path.Combine(webRoot, "Upload", "books", policy.DirectoryName);

        Directory.CreateDirectory(uploadDirectory);

        var storedFileName = $"{ToSafeFileName(bookTitle)}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(uploadDirectory, storedFileName);

        await using (var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            await file.CopyToAsync(destination, cancellationToken);
        }

        var relativeUrl = $"/Upload/books/{policy.DirectoryName}/{storedFileName}";
        var publicUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{relativeUrl}";

        return Created(publicUrl, new AdminAssetUploadDto
        {
            // Persist a host-independent path. Request.Host is often localhost when
            // the admin and API run on the same development computer.
            Url = relativeUrl,
            FileName = file.FileName,
            ContentType = expectedContentType,
            Size = file.Length
        });
    }

    private static async Task<bool> HasValidSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];

        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension switch
        {
            ".pdf" => bytesRead >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" or ".jpeg" => bytesRead >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 &&
                header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytesRead >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".mp3" => bytesRead >= 3 &&
                (header.AsSpan(0, 3).SequenceEqual("ID3"u8) ||
                 (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)),
            _ => false
        };
    }

    private static bool HasExpectedContentType(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) ||
        expected == "audio/mpeg" && actual.ToLowerInvariant() is
            "audio/mp3" or
            "audio/mpeg3" or
            "audio/x-mpeg-3" or
            "application/octet-stream";

    private static string ToSafeFileName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "book";

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var cleaned = new string(title.Trim()
            .Where(character => !invalidCharacters.Contains(character) &&
                                (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) || character is '-' or '_'))
            .ToArray());
        cleaned = string.Join('-', cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (cleaned.Length > 80) cleaned = cleaned[..80].TrimEnd('-', '_');
        return string.IsNullOrWhiteSpace(cleaned) ? "book" : cleaned;
    }

    private sealed record UploadPolicy(
        string DirectoryName,
        long MaximumSize,
        IReadOnlyDictionary<string, string> AllowedContentTypes);
}
