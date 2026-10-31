using Microsoft.EntityFrameworkCore;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Data.Services;

public sealed class SQLUserAccountRepository : IUserAccountRepository
{
    private readonly DbContextAnti _db;

    public SQLUserAccountRepository(DbContextAnti db) => _db = db;

    public Task<AppUser?> FindByNormalizedEmailAsync(string normalizedEmail) =>
        _db.AppUsers.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);

    public Task<AppUser?> FindByIdAsync(int userId) =>
        _db.AppUsers.SingleOrDefaultAsync(user => user.Id == userId);

    public async Task AddUserAsync(AppUser user)
    {
        await _db.AppUsers.AddAsync(user);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

    public async Task AddRefreshTokenAsync(UserRefreshToken token)
    {
        await _db.UserRefreshTokens.AddAsync(token);
        await _db.SaveChangesAsync();
    }

    public Task<UserRefreshToken?> FindRefreshTokenAsync(string tokenHash) =>
        _db.UserRefreshTokens.Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

    public async Task<IReadOnlyList<BooksDTO>> GetFavoritesAsync(int userId) =>
        await _db.UserFavorites
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .Select(favorite => new BooksDTO
            {
                Id = favorite.Book.Id,
                Title = favorite.Book.Title,
                Url = favorite.Book.Url,
                ImageUrl = favorite.Book.ImageUrl,
                Description = favorite.Book.Description,
                Date = favorite.Book.Date,
                FileSize = favorite.Book.FileSize,
                PagesCount = favorite.Book.PagesCount,
                AuthorId = favorite.Book.AuthorId,
                AuthorName = favorite.Book.Author == null ? string.Empty : favorite.Book.Author.Name + " " + favorite.Book.Author.Family,
                LikesCount = favorite.Book.LikesCount,
                Score = favorite.Book.Score,
                IsAudioBook = favorite.Book.IsAudioBook,
                IsSuggested = favorite.Book.IsSuggested
            })
            .ToListAsync();

    public Task<bool> IsFavoriteAsync(int userId, int bookId) =>
        _db.UserFavorites.AnyAsync(favorite => favorite.UserId == userId && favorite.BookId == bookId);

    public async Task SetFavoriteAsync(int userId, int bookId, bool isFavorite)
    {
        var favorite = await _db.UserFavorites.FindAsync(userId, bookId);
        if (isFavorite && favorite is null)
        {
            if (!await _db.Books.AnyAsync(book => book.Id == bookId))
                throw new KeyNotFoundException("Book not found.");

            await _db.UserFavorites.AddAsync(new UserFavorite
            {
                UserId = userId,
                BookId = bookId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else if (!isFavorite && favorite is not null)
        {
            _db.UserFavorites.Remove(favorite);
        }

        await _db.SaveChangesAsync();
    }

    public Task<ReadingProgress?> GetProgressAsync(int userId, int bookId) =>
        _db.ReadingProgresses.AsNoTracking()
            .SingleOrDefaultAsync(progress => progress.UserId == userId && progress.BookId == bookId);

    public async Task UpsertProgressAsync(int userId, int bookId, ReadingProgressDto progress)
    {
        if (!await _db.Books.AnyAsync(book => book.Id == bookId))
            throw new KeyNotFoundException("Book not found.");

        var entity = await _db.ReadingProgresses.FindAsync(userId, bookId);
        if (entity is null)
        {
            entity = new ReadingProgress { UserId = userId, BookId = bookId };
            await _db.ReadingProgresses.AddAsync(entity);
        }

        entity.PositionPercent = Math.Clamp(progress.PositionPercent, 0, 100);
        entity.CurrentPage = progress.CurrentPage;
        entity.TotalPages = progress.TotalPages;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
