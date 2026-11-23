using System.Security.Cryptography;
using System.Text;
#if ANDROID
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidBitmapFactory = Android.Graphics.BitmapFactory;
#endif

namespace TasnimBook.MAUI.Services;

/// <summary>
/// Downloads category covers through the same managed HTTP stack as the API and returns
/// local file image sources. This avoids Android image-loader differences for
/// percent-encoded Persian URLs and keeps the downloaded file for later launches.
/// </summary>
public sealed class CategoryCoverCacheService : IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly SemaphoreSlim _downloadSlots = new(3, 3);
    private readonly string _cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "category-covers");

    public async Task<ImageSource?> GetAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        try
        {
            var path = await GetLocalPathAsync(uri, cancellationToken);
            // FileImageSource may interpret an absolute cache path as an Android resource.
            // A synchronous local stream is reliable on both Android and Windows.
            return ImageSource.FromStream(() =>
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Category cover failed: {exception.Message}");
            return null;
        }
    }

    private async Task<string> GetLocalPathAsync(Uri uri, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_cacheDirectory);
        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) extension = ".img";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri)));
        var destination = Path.Combine(_cacheDirectory, hash + extension);
        if (IsUsable(destination)) return await PrepareForDisplayAsync(destination, cancellationToken);

        await _downloadSlots.WaitAsync(cancellationToken);
        string? temporary = null;
        try
        {
            if (IsUsable(destination)) return await PrepareForDisplayAsync(destination, cancellationToken);

            using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The category cover response is not an image.");

            temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = File.Create(temporary))
                await input.CopyToAsync(output, cancellationToken);

            File.Move(temporary, destination, overwrite: true);
            temporary = null;
            return await PrepareForDisplayAsync(destination, cancellationToken);
        }
        finally
        {
            if (temporary is not null && File.Exists(temporary)) File.Delete(temporary);
            _downloadSlots.Release();
        }
    }

    private static bool IsUsable(string path) => File.Exists(path) && new FileInfo(path).Length > 0;

    private static async Task<string> PrepareForDisplayAsync(string originalPath, CancellationToken cancellationToken)
    {
#if ANDROID
        var thumbnailPath = originalPath + ".thumb.jpg";
        if (IsUsable(thumbnailPath)) return thumbnailPath;

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var bounds = new AndroidBitmapFactory.Options { InJustDecodeBounds = true };
            AndroidBitmapFactory.DecodeFile(originalPath, bounds);
            if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
                throw new InvalidDataException("ابعاد تصویر جلد معتبر نیست.");

            var sampleSize = 1;
            while (bounds.OutWidth / sampleSize > 720 || bounds.OutHeight / sampleSize > 960)
                sampleSize *= 2;

            using var options = new AndroidBitmapFactory.Options
            {
                InSampleSize = sampleSize,
                InPreferredConfig = AndroidBitmap.Config.Rgb565
            };
            using var decoded = AndroidBitmapFactory.DecodeFile(originalPath, options)
                ?? throw new InvalidDataException("خواندن تصویر جلد ناموفق بود.");

            const int maxWidth = 360;
            const int maxHeight = 480;
            var scale = Math.Min(1d, Math.Min(maxWidth / (double)decoded.Width, maxHeight / (double)decoded.Height));
            var width = Math.Max(1, (int)Math.Round(decoded.Width * scale));
            var height = Math.Max(1, (int)Math.Round(decoded.Height * scale));
            using var thumbnail = AndroidBitmap.CreateScaledBitmap(decoded, width, height, true)
                ?? throw new InvalidOperationException("ساخت تصویر کوچک جلد ناموفق بود.");
            using var output = File.Create(thumbnailPath);
            if (!thumbnail.Compress(AndroidBitmap.CompressFormat.Jpeg, 84, output))
                throw new InvalidOperationException("ذخیره تصویر کوچک جلد ناموفق بود.");
        }, cancellationToken);
        return thumbnailPath;
#else
        await Task.CompletedTask;
        return originalPath;
#endif
    }

    public void Dispose()
    {
        _client.Dispose();
        _downloadSlots.Dispose();
    }
}
