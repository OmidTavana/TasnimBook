using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("appuser")]
public sealed class AppUser
{
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(254), Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(254), Column("normalizedemail")]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required, MaxLength(80), Column("displayname")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, Column("passwordhash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("createdatutc")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("lastloginatutc")]
    public DateTime? LastLoginAtUtc { get; set; }

    [Column("isactive")]
    public bool IsActive { get; set; } = true;

    [Required, MaxLength(32), Column("role")]
    public string Role { get; set; } = UserRoles.User;

    [Required, MaxLength(32), Column("adminstatus")]
    public string AdminStatus { get; set; } = AdminApprovalStatuses.NotApplicable;

    [MaxLength(500), Column("adminreviewnote")]
    public string? AdminReviewNote { get; set; }

    [Column("adminreviewedatutc")]
    public DateTime? AdminReviewedAtUtc { get; set; }

    [Column("adminreviewedbyuserid")]
    public int? AdminReviewedByUserId { get; set; }

    public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
    public ICollection<BookRating> Ratings { get; set; } = new List<BookRating>();
    public ICollection<BookLike> Likes { get; set; } = new List<BookLike>();
    public ICollection<UserLibraryBook> LibraryBooks { get; set; } = new List<UserLibraryBook>();
    public ICollection<BookDownload> Downloads { get; set; } = new List<BookDownload>();
}

public static class UserRoles
{
    public const string User = "User";
    public const string ContentAdmin = "ContentAdmin";
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrators = ContentAdmin + "," + SuperAdmin;
}

public static class AdminApprovalStatuses
{
    public const string NotApplicable = "NotApplicable";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
