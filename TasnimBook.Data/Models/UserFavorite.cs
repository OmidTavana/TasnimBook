using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("userfavorite")]
public sealed class UserFavorite
{
    [Column("userid")]
    public int UserId { get; set; }

    [Column("bookid")]
    public int BookId { get; set; }

    [Column("createdatutc")]
    public DateTime CreatedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
