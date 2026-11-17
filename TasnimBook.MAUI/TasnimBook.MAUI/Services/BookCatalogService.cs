using TasnimBook.Data;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Services;

/// <summary>Single UI-facing gateway for the Core REST repositories.</summary>
public sealed class BookCatalogService
{
    private readonly ApiSessionService _session;

    public BookCatalogService(ApiSessionService session) => _session = session;

    public async Task<IReadOnlyList<BooksDTO>> GetNewAsync(int count = 12) =>
        await ReadAsync(async () => await (await _session.GetRepositoryAsync()).IBooks.GetNewBooks(1, count));

    public async Task<IReadOnlyList<BooksDTO>> GetSuggestedAsync(int count = 12) =>
        await ReadAsync(async () => await (await _session.GetRepositoryAsync()).IBooks.GetSuggestedBooks(1, count));

    public async Task<IReadOnlyList<BooksDTO>> GetAudioAsync(int count = 30)
    {
        var repository = await _session.GetRepositoryAsync();
        var audioBooks = await ReadAsync(async () => await repository.IBooks.GetAudioBooks(1, count));
        if (audioBooks.Count > 0) return audioBooks;

        // Keeps the section working while an older API deployment is being replaced.
        var legacyBooks = await ReadAsync(async () => await repository.IBooks.GetRandomBooks(1, Math.Max(100, count)));
        return legacyBooks.Where(book => book.IsAudioBook == true).Take(count).ToList();
    }

    public async Task<IReadOnlyList<BooksDTO>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<BooksDTO>();

        return await ReadAsync(async () => await (await _session.GetRepositoryAsync()).IBooks.Search(query.Trim()));
    }

    public async Task<Book> GetBookAsync(int id)
    {
        var book = await (await _session.GetRepositoryAsync()).IBooks.GetBook(id);
        book.Url = ApiConfiguration.ResolveResourceUrl(book.Url) ?? book.Url;
        book.ImageUrl = ApiConfiguration.ResolveResourceUrl(book.ImageUrl) ?? book.ImageUrl;
        return book;
    }

    private static async Task<IReadOnlyList<BooksDTO>> ReadAsync(Func<Task<IEnumerable<BooksDTO>>> fetch)
    {
        try
        {
            return (await fetch()).Select(NormalizeAssetUrls).ToList();
        }
        catch
        {
            // A quiet empty state is more useful on a phone than an unhandled network exception.
            return Array.Empty<BooksDTO>();
        }
    }

    internal static BooksDTO NormalizeAssetUrls(BooksDTO book)
    {
        book.Url = ApiConfiguration.ResolveResourceUrl(book.Url) ?? book.Url;
        book.ImageUrl = ApiConfiguration.ResolveResourceUrl(book.ImageUrl) ?? book.ImageUrl;
        return book;
    }
}
