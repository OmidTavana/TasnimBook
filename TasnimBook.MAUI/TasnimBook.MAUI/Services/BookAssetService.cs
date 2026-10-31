using System.Net.Http.Headers;
using TasnimBook.Data;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Services;

public sealed class BookAssetService
{
    private readonly HttpClient _client;
    private readonly string _downloadsDirectory;
    private readonly string _coversDirectory;

    public BookAssetService()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("TasnimBook/1.0");
        _downloadsDirectory = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
        _coversDirectory = Path.Combine(FileSystem.CacheDirectory, "BookCovers");
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_coversDirectory);
    }

    public string GetPdfPath(int bookId) => Path.Combine(_downloadsDirectory, $"book-{bookId}.pdf");

    public async Task<string?> GetCoverPathAsync(int bookId, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)) return null;
        var path = Path.Combine(_coversDirectory, $"book-{bookId}.jpg");
        if (File.Exists(path) && new FileInfo(path).Length > 0) return path;

        var temporaryPath = path + ".part";
        try
        {
            using var coverTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            coverTimeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, coverTimeout.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                throw new InvalidDataException("پاسخ تصویر معتبر نیست.");
            await using var source = await response.Content.ReadAsStreamAsync(coverTimeout.Token);
            await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                await source.CopyToAsync(target, coverTimeout.Token);
            File.Move(temporaryPath, path, true);
            return path;
        }
        catch
        {
            TryDelete(temporaryPath);
            return null;
        }
    }

    public async Task<string> EnsurePdfAsync(
        Book book,
        BooksDTO dto,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var finalPath = GetPdfPath(book.Id);
        if (await IsPdfAsync(finalPath, cancellationToken))
        {
            progress?.Report(1);
            return finalPath;
        }

        var url = string.IsNullOrWhiteSpace(book.Url) ? dto.Url : book.Url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("لینک PDF این کتاب معتبر نیست.");

        var temporaryPath = finalPath + ".part";
        TryDelete(temporaryPath);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long downloaded = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    downloaded += read;
                    if (totalBytes is > 0) progress?.Report((double)downloaded / totalBytes.Value);
                }
                await target.FlushAsync(cancellationToken);
            }

            if (!await IsPdfAsync(temporaryPath, cancellationToken))
                throw new InvalidDataException("فایل دریافت‌شده PDF معتبر نیست.");

            File.Move(temporaryPath, finalPath, true);
            progress?.Report(1);
            return finalPath;
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static async Task<bool> IsPdfAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 5) return false;
        var signature = new byte[5];
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 5, true);
        return await stream.ReadAsync(signature, cancellationToken) == 5
            && signature[0] == (byte)'%' && signature[1] == (byte)'P' && signature[2] == (byte)'D'
            && signature[3] == (byte)'F' && signature[4] == (byte)'-';
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}
