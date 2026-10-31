using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Platforms.Windows;

public sealed class WindowsLocalPdfDocument : ILocalPdfDocument
{
    private PdfDocument? _document;
    public int PageCount => checked((int)(_document?.PageCount ?? 0));

    public async Task OpenAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        _document = await PdfDocument.LoadFromFileAsync(file);
    }

    public async Task<byte[]> RenderPageAsync(int pageIndex, int targetWidth)
    {
        if (_document is null) throw new InvalidOperationException("PDF باز نشده است.");
        using var page = _document.GetPage((uint)pageIndex);
        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, new PdfPageRenderOptions { DestinationWidth = (uint)Math.Max(320, targetWidth) });
        var length = checked((int)stream.Size);
        var bytes = new byte[length];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)length);
        reader.ReadBytes(bytes);
        return bytes;
    }

    public ValueTask DisposeAsync()
    {
        _document = null;
        return ValueTask.CompletedTask;
    }
}
