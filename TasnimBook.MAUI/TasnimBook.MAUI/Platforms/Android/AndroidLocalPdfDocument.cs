#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Platforms.AndroidPlatform;

public sealed class AndroidLocalPdfDocument : ILocalPdfDocument
{
    private ParcelFileDescriptor? _descriptor;
    private PdfRenderer? _renderer;
    public int PageCount => _renderer?.PageCount ?? 0;

    public Task OpenAsync(string path)
    {
        _descriptor = ParcelFileDescriptor.Open(new Java.IO.File(path), ParcelFileMode.ReadOnly)
            ?? throw new FileNotFoundException("فایل PDF پیدا نشد.", path);
        _renderer = new PdfRenderer(_descriptor);
        return Task.CompletedTask;
    }

    public Task<byte[]> RenderPageAsync(int pageIndex, int targetWidth)
    {
        if (_renderer is null) throw new InvalidOperationException("PDF باز نشده است.");
        using var page = _renderer.OpenPage(pageIndex);
        var width = Math.Max(320, targetWidth);
        var height = Math.Max(1, (int)Math.Round(width * (double)page.Height / page.Width));
        using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888)
            ?? throw new InvalidOperationException("ساخت تصویر صفحه PDF ناموفق بود.");
        bitmap.EraseColor(Android.Graphics.Color.White);
        page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);
        using var stream = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
        return Task.FromResult(stream.ToArray());
    }

    public ValueTask DisposeAsync()
    {
        _renderer?.Dispose();
        _descriptor?.Dispose();
        _renderer = null;
        _descriptor = null;
        return ValueTask.CompletedTask;
    }
}
#endif
