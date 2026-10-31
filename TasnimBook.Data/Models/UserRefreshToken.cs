using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("userrefreshtoken")]
public sealed class UserRefreshToken
{
    [Column("id")]
    public int Id { get; set; }

    [Column("userid")]
    public int UserId { get; set; }

    [Required, MaxLength(64), Column("tokenhash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("createdatutc")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("expiresatutc")]
    public DateTime ExpiresAtUtc { get; set; }

    [Column("revokedatutc")]
    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(64), Column("replacedbytokenhash")]
    public string? ReplacedByTokenHash { get; set; }

    [MaxLength(120), Column("devicename")]
    public string? DeviceName { get; set; }

    public AppUser User { get; set; } = null!;
}
