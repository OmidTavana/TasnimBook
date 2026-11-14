using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;

namespace webapicore.Services;

public interface IBookEngagementService
{
    Task<BookEngagementDto> GetAsync(int userId, int bookId, CancellationToken cancellationToken);
    Task<BookEngagementDto> RateAsync(int userId, int bookId, byte? value, CancellationToken cancellationToken);
    Task<BookEngagementDto> LikeAsync(int userId, int bookId, bool liked, CancellationToken cancellationToken);
    Task RecordLibraryEventAsync(int userId, int bookId, string eventType, CancellationToken cancellationToken);
    Task<IReadOnlyList<BooksDTO>> GetLibraryAsync(int userId, CancellationToken cancellationToken);
    Task RemoveFromLibraryAsync(int userId, int bookId, CancellationToken cancellationToken);
}

public sealed class BookEngagementService(DbContextAnti db) : IBookEngagementService
{
    public async Task<BookEngagementDto> GetAsync(int userId, int bookId, CancellationToken cancellationToken)
    {
        if (!await db.Books.AsNoTracking().AnyAsync(book => book.Id == bookId, cancellationToken))
            throw new KeyNotFoundException("Book not found.");

        var ratings = db.BookRatings.AsNoTracking().Where(rating => rating.BookId == bookId);
        return new BookEngagementDto
        {
            BookId = bookId,
            RatingCount = await ratings.CountAsync(cancellationToken),
            AverageRating = await ratings.Select(rating => (double?)rating.Value).AverageAsync(cancellationToken) ?? 0,
            CurrentUserRating = await ratings.Where(rating => rating.UserId == userId)
                .Select(rating => (byte?)rating.Value).SingleOrDefaultAsync(cancellationToken),
            LikeCount = await db.BookLikes.CountAsync(like => like.BookId == bookId, cancellationToken),
            IsLikedByCurrentUser = await db.BookLikes.AnyAsync(like => like.BookId == bookId && like.UserId == userId, cancellationToken),
            IsInLibrary = await db.UserLibraryBooks.AnyAsync(item => item.BookId == bookId && item.UserId == userId, cancellationToken)
        };
    }

    public async Task<BookEngagementDto> RateAsync(int userId, int bookId, byte? value, CancellationToken cancellationToken)
    {
        await EnsureBookAsync(bookId, cancellationToken);
        var rating = await db.BookRatings.FindAsync([userId, bookId], cancellationToken);
        if (value is null)
        {
            if (rating is not null) db.BookRatings.Remove(rating);
        }
        else
        {
            if (value is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(value));
            if (rating is null)
            {
                rating = new BookRating { UserId = userId, BookId = bookId, CreatedAtUtc = DateTime.UtcNow };
                await db.BookRatings.AddAsync(rating, cancellationToken);
            }
            rating.Value = value.Value;
            rating.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, bookId, cancellationToken);
    }

    public async Task<BookEngagementDto> LikeAsync(int userId, int bookId, bool liked, CancellationToken cancellationToken)
    {
        await EnsureBookAsync(bookId, cancellationToken);
        var like = await db.BookLikes.FindAsync([userId, bookId], cancellationToken);
        if (liked && like is null)
            await db.BookLikes.AddAsync(new BookLike { UserId = userId, BookId = bookId, CreatedAtUtc = DateTime.UtcNow }, cancellationToken);
        else if (!liked && like is not null)
            db.BookLikes.Remove(like);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, bookId, cancellationToken);
    }

    public async Task RecordLibraryEventAsync(int userId, int bookId, string eventType, CancellationToken cancellationToken)
    {
        await EnsureBookAsync(bookId, cancellationToken);
        var now = DateTime.UtcNow;
        var entry = await db.UserLibraryBooks.FindAsync([userId, bookId], cancellationToken);
        if (entry is null)
        {
            entry = new UserLibraryBook { UserId = userId, BookId = bookId, AddedAtUtc = now };
            await db.UserLibraryBooks.AddAsync(entry, cancellationToken);
        }

        if (eventType == "Read") entry.LastReadAtUtc = now;
        else if (eventType == "Download")
        {
            entry.LastDownloadedAtUtc = now;
            if (!await db.BookDownloads.AnyAsync(item => item.UserId == userId && item.BookId == bookId, cancellationToken))
                await db.BookDownloads.AddAsync(new BookDownload { UserId = userId, BookId = bookId, DownloadedAtUtc = now }, cancellationToken);
        }
        else throw new ArgumentException("Unsupported library event.", nameof(eventType));

        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            // Composite keys make simultaneous successful-download callbacks idempotent.
            db.ChangeTracker.Clear();
        }
    }

    public async Task<IReadOnlyList<BooksDTO>> GetLibraryAsync(int userId, CancellationToken cancellationToken) =>
        await db.UserLibraryBooks.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.LastReadAtUtc ?? item.LastDownloadedAtUtc ?? item.AddedAtUtc)
            .Select(item => new BooksDTO
            {
                Id = item.Book.Id, Title = item.Book.Title, Url = item.Book.Url, ImageUrl = item.Book.ImageUrl,
                Description = item.Book.Description, Date = item.Book.Date, FileSize = item.Book.FileSize,
                PagesCount = item.Book.PagesCount, AuthorId = item.Book.AuthorId,
                AuthorName = item.Book.Author == null ? string.Empty : item.Book.Author.Name + " " + item.Book.Author.Family,
                IsAudioBook = item.Book.IsAudioBook, IsSuggested = item.Book.IsSuggested
            }).ToListAsync(cancellationToken);

    public async Task RemoveFromLibraryAsync(int userId, int bookId, CancellationToken cancellationToken)
    {
        var entry = await db.UserLibraryBooks.FindAsync([userId, bookId], cancellationToken);
        if (entry is not null)
        {
            db.UserLibraryBooks.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureBookAsync(int bookId, CancellationToken cancellationToken)
    {
        if (!await db.Books.AnyAsync(book => book.Id == bookId, cancellationToken))
            throw new KeyNotFoundException("Book not found.");
    }
}
