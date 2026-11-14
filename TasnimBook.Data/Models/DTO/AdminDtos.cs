namespace TasnimBook.Data.Models.DTO;

public sealed class DashboardDto
{
    public int TotalBooks { get; set; }
    public int TotalAuthors { get; set; }
    public int TotalCategories { get; set; }
    public int TotalRegisteredUsers { get; set; }
    public int TotalDownloads { get; set; }
    public int TotalLikes { get; set; }
    public int TotalRatings { get; set; }
    public int PendingAdministratorRegistrations { get; set; }
    public IReadOnlyList<RankedBookDto> PopularBooks { get; set; } = [];
    public IReadOnlyList<RankedBookDto> MostDownloadedBooks { get; set; } = [];
    public IReadOnlyList<RankedBookDto> MostLikedBooks { get; set; } = [];
    public IReadOnlyList<RankedBookDto> HighestRatedBooks { get; set; } = [];
}

public sealed class RankedBookDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public int LikeCount { get; set; }
    public int RatingCount { get; set; }
    public double AverageRating { get; set; }
    public double PopularityScore { get; set; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class AdminUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public string AdminStatus { get; set; } = AdminApprovalStatuses.NotApplicable;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
