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
        var books = await ReadAsync(async () => await (await _session.GetRepositoryAsync()).IBooks.GetRandomBooks(1, count));
        return books.Where(book => book.IsAudioBook == true).ToList();
    }

    public async Task<IReadOnlyList<BooksDTO>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<BooksDTO>();

        return await ReadAsync(async () => await (await _session.GetRepositoryAsync()).IBooks.Search(query.Trim()));
    }

    public async Task<Book> GetBookAsync(int id) => await (await _session.GetRepositoryAsync()).IBooks.GetBook(id);

    private static async Task<IReadOnlyList<BooksDTO>> ReadAsync(Func<Task<IEnumerable<BooksDTO>>> fetch)
    {
        try
        {
            return (await fetch()).ToList();
        }
        catch
        {
            // A quiet empty state is more useful on a phone than an unhandled network exception.
            return Array.Empty<BooksDTO>();
        }
    }
}
