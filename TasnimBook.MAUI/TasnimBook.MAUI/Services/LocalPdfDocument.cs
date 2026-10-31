namespace TasnimBook.MAUI.Services;

public interface ILocalPdfDocument : IAsyncDisposable
{
    int PageCount { get; }
    Task OpenAsync(string path);
    Task<byte[]> RenderPageAsync(int pageIndex, int targetWidth);
}

public static class LocalPdfDocumentFactory
{
    public static ILocalPdfDocument Create()
    {
#if ANDROID
        return new TasnimBook.MAUI.Platforms.AndroidPlatform.AndroidLocalPdfDocument();
#elif WINDOWS
        return new TasnimBook.MAUI.Platforms.Windows.WindowsLocalPdfDocument();
#else
        return new UnsupportedLocalPdfDocument();
#endif
    }

    private sealed class UnsupportedLocalPdfDocument : ILocalPdfDocument
    {
        public int PageCount => 0;
        public Task OpenAsync(string path) => throw new PlatformNotSupportedException("نمایش محلی PDF در این پلتفرم هنوز پشتیبانی نمی‌شود.");
        public Task<byte[]> RenderPageAsync(int pageIndex, int targetWidth) => throw new PlatformNotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
