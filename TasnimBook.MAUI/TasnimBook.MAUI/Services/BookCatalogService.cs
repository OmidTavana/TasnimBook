using TasnimBook.Data;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Services;

/// <summary>Single UI-facing gateway for the Core REST repositories.</summary>
public sealed class BookCatalogService
{
    private readonly ApiSessionService _session;

    public BookCatalogService(ApiSessionService session) => _session = session;

    public async Task<IReadOnlyList<BooksDTO>> GetNewAsync(int count = 12) =>
        await ReadAsync(repository => repository.IBooks.GetNewBooks(1, count));

    public async Task<IReadOnlyList<BooksDTO>> GetSuggestedAsync(int count = 12) =>
        await ReadAsync(repository => repository.IBooks.GetSuggestedBooks(1, count));

    public async Task<IReadOnlyList<BooksDTO>> GetAudioAsync(int count = 30)
    {
        var audioBooks = await ReadAsync(repository => repository.IBooks.GetAudioBooks(1, count));
        if (audioBooks.Count > 0) return audioBooks;

        // Keeps the section working while an older API deployment is being replaced.
        var legacyBooks = await ReadAsync(repository => repository.IBooks.GetRandomBooks(1, Math.Max(100, count)));
        return legacyBooks.Where(book => book.IsAudioBook == true).Take(count).ToList();
    }

    public async Task<IReadOnlyList<BooksDTO>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<BooksDTO>();

        return await ReadAsync(repository => repository.IBooks.Search(query.Trim()));
    }

    public async Task<IReadOnlyList<BookCat>> GetCategoriesAsync()
    {
        return await ExecuteWithRetryAsync(async repository =>
            (await repository.ICategory.List())
                .Where(category => category.Id > 0 && !string.IsNullOrWhiteSpace(category.CategoryName))
                .OrderBy(category => category.CategoryName)
                .ToList());
    }

    public async Task<IReadOnlyList<BooksDTO>> GetAllAsync() =>
        await ReadAsync(repository => repository.IBooks.List());

    public async Task<IReadOnlyList<BooksDTO>> GetByCategoryAsync(int categoryId) =>
        await ReadAsync(repository => repository.IBooks.ListByCatergory(categoryId));

    public async Task<Book> GetBookAsync(int id)
    {
        var book = await ExecuteWithRetryAsync(repository => repository.IBooks.GetBook(id));
        book.Url = ApiConfiguration.ResolveResourceUrl(book.Url) ?? book.Url;
        book.ImageUrl = ApiConfiguration.ResolveResourceUrl(book.ImageUrl) ?? book.ImageUrl;
        return book;
    }

    private async Task<IReadOnlyList<BooksDTO>> ReadAsync(Func<IRepository, Task<IEnumerable<BooksDTO>>> fetch)
    {
        return await ExecuteWithRetryAsync(async repository =>
            (await fetch(repository)).Select(NormalizeAssetUrls).ToList());
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<IRepository, Task<T>> action)
    {
        for (var attempt = 0; ; attempt++)
        {
            var repository = await _session.GetRepositoryAsync();
            try
            {
                return await action(repository);
            }
            catch when (attempt == 0)
            {
                // Only invalidate the exact session that failed. Another concurrent request
                // may already have replaced it with a healthy connection.
                _session.Reset(repository);
            }
        }
    }

    internal static BooksDTO NormalizeAssetUrls(BooksDTO book)
    {
        book.Url = ApiConfiguration.ResolveResourceUrl(book.Url) ?? book.Url;
        book.ImageUrl = ApiConfiguration.ResolveResourceUrl(book.ImageUrl) ?? book.ImageUrl;
        return book;
    }
}
