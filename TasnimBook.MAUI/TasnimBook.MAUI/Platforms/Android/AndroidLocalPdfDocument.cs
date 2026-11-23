#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Platforms.AndroidPlatform;

public sealed class AndroidLocalPdfDocument : ILocalPdfDocument
{
    private readonly SemaphoreSlim _renderGate = new(1, 1);
    private string? _path;
    private int _pageCount;
    public int PageCount => _pageCount;

    public Task OpenAsync(string path)
    {
        using var descriptor = ParcelFileDescriptor.Open(new Java.IO.File(path), ParcelFileMode.ReadOnly)
            ?? throw new FileNotFoundException("فایل PDF پیدا نشد.", path);
        using var renderer = new PdfRenderer(descriptor);
        _pageCount = renderer.PageCount;
        _path = path;
        return Task.CompletedTask;
    }

    public async Task<byte[]> RenderPageAsync(int pageIndex, int targetWidth)
    {
        await _renderGate.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                var path = _path ?? throw new InvalidOperationException("PDF باز نشده است.");
                using var descriptor = ParcelFileDescriptor.Open(new Java.IO.File(path), ParcelFileMode.ReadOnly)
                    ?? throw new FileNotFoundException("فایل PDF پیدا نشد.", path);
                using var renderer = new PdfRenderer(descriptor);
                if (pageIndex < 0 || pageIndex >= renderer.PageCount)
                    throw new ArgumentOutOfRangeException(nameof(pageIndex));

                // A fresh renderer per page avoids native PdfRenderer state leaking from
                // one page to the next on some Android/OEM Release builds.
                using var page = renderer.OpenPage(pageIndex);
                var width = Math.Clamp(targetWidth, 480, 1280);
                var height = Math.Max(1, (int)Math.Round(width * (double)page.Height / Math.Max(1, page.Width)));

                // Keep the native bitmap below roughly 10 MB (ARGB_8888). Extremely tall
                // PDF pages otherwise cause an Android process kill on the next page.
                const double maxPixels = 2_500_000;
                if ((double)width * height > maxPixels)
                {
                    var scale = Math.Sqrt(maxPixels / (width * (double)height));
                    width = Math.Max(320, (int)(width * scale));
                    height = Math.Max(1, (int)(height * scale));
                }

                using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888)
                    ?? throw new InvalidOperationException("ساخت تصویر صفحه PDF ناموفق بود.");
                bitmap.EraseColor(Android.Graphics.Color.White);
                page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);
                using var stream = new MemoryStream();
                if (!bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream))
                    throw new InvalidOperationException("تبدیل صفحه PDF به تصویر ناموفق بود.");
                return stream.ToArray();
            });
        }
        finally
        {
            _renderGate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _path = null;
        _pageCount = 0;
        return ValueTask.CompletedTask;
    }
}
#endif
