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

    public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
}
