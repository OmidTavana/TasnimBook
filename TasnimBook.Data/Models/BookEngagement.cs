using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("bookrating")]
public sealed class BookRating
{
    [Column("userid")] public int UserId { get; set; }
    [Column("bookid")] public int BookId { get; set; }
    [Range(1, 5), Column("value")] public byte Value { get; set; }
    [Column("createdatutc")] public DateTime CreatedAtUtc { get; set; }
    [Column("updatedatutc")] public DateTime UpdatedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

[Table("booklike")]
public sealed class BookLike
{
    [Column("userid")] public int UserId { get; set; }
    [Column("bookid")] public int BookId { get; set; }
    [Column("createdatutc")] public DateTime CreatedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

[Table("userlibrarybook")]
public sealed class UserLibraryBook
{
    [Column("userid")] public int UserId { get; set; }
    [Column("bookid")] public int BookId { get; set; }
    [Column("addedatutc")] public DateTime AddedAtUtc { get; set; }
    [Column("lastreadatutc")] public DateTime? LastReadAtUtc { get; set; }
    [Column("lastdownloadedatutc")] public DateTime? LastDownloadedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

// A row represents one counted download. The composite key implements the documented
// first-successful-download-per-user/book policy and makes concurrent retries idempotent.
[Table("bookdownload")]
public sealed class BookDownload
{
    [Column("userid")] public int UserId { get; set; }
    [Column("bookid")] public int BookId { get; set; }
    [Column("downloadedatutc")] public DateTime DownloadedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
