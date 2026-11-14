using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController, ApiKeyAuth, Authorize(Roles = UserRoles.Administrators), Route("api/[controller]/[action]")]
public sealed class DashboardController(DbContextAnti db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Overview(CancellationToken cancellationToken)
    {
        var ranked = db.Books.AsNoTracking().Select(book => new RankedBookDto
        {
            BookId = book.Id, Title = book.Title, ImageUrl = book.ImageUrl,
            Author = book.Author == null ? string.Empty : book.Author.Name + " " + book.Author.Family,
            DownloadCount = db.BookDownloads.Count(item => item.BookId == book.Id),
            LikeCount = db.BookLikes.Count(item => item.BookId == book.Id),
            RatingCount = db.BookRatings.Count(item => item.BookId == book.Id),
            AverageRating = db.BookRatings.Where(item => item.BookId == book.Id).Select(item => (double?)item.Value).Average() ?? 0,
            PopularityScore = db.BookDownloads.Count(item => item.BookId == book.Id) * 3
                + db.BookLikes.Count(item => item.BookId == book.Id) * 2
                + db.BookRatings.Count(item => item.BookId == book.Id)
        });

        return Ok(new DashboardDto
        {
            TotalBooks = await db.Books.CountAsync(cancellationToken),
            TotalAuthors = await db.Authors.CountAsync(cancellationToken),
            TotalCategories = await db.BookCategories.CountAsync(cancellationToken),
            TotalRegisteredUsers = await db.AppUsers.CountAsync(cancellationToken),
            TotalDownloads = await db.BookDownloads.CountAsync(cancellationToken),
            TotalLikes = await db.BookLikes.CountAsync(cancellationToken),
            TotalRatings = await db.BookRatings.CountAsync(cancellationToken),
            PendingAdministratorRegistrations = await db.AppUsers.CountAsync(user => user.AdminStatus == AdminApprovalStatuses.PendingApproval, cancellationToken),
            PopularBooks = await ranked.OrderByDescending(item => item.PopularityScore).ThenBy(item => item.Title).Take(10).ToListAsync(cancellationToken),
            MostDownloadedBooks = await ranked.OrderByDescending(item => item.DownloadCount).ThenBy(item => item.Title).Take(10).ToListAsync(cancellationToken),
            MostLikedBooks = await ranked.OrderByDescending(item => item.LikeCount).ThenBy(item => item.Title).Take(10).ToListAsync(cancellationToken),
            // Minimum three ratings avoids presenting a single rating as a reliable top rank.
            HighestRatedBooks = await ranked.Where(item => item.RatingCount >= 3)
                .OrderByDescending(item => item.AverageRating).ThenByDescending(item => item.RatingCount).Take(10).ToListAsync(cancellationToken)
        });
    }
}
