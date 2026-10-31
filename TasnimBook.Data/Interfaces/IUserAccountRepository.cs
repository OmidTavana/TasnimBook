using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Data.Interfaces;

public interface IUserAccountRepository
{
    Task<AppUser?> FindByNormalizedEmailAsync(string normalizedEmail);
    Task<AppUser?> FindByIdAsync(int userId);
    Task AddUserAsync(AppUser user);
    Task SaveChangesAsync();

    Task AddRefreshTokenAsync(UserRefreshToken token);
    Task<UserRefreshToken?> FindRefreshTokenAsync(string tokenHash);

    Task<IReadOnlyList<BooksDTO>> GetFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int bookId);
    Task SetFavoriteAsync(int userId, int bookId, bool isFavorite);

    Task<ReadingProgress?> GetProgressAsync(int userId, int bookId);
    Task UpsertProgressAsync(int userId, int bookId, ReadingProgressDto progress);
}
