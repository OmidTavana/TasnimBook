using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("readingprogress")]
public sealed class ReadingProgress
{
    [Column("userid")]
    public int UserId { get; set; }

    [Column("bookid")]
    public int BookId { get; set; }

    [Column("positionpercent", TypeName = "decimal(5,2)")]
    public decimal PositionPercent { get; set; }

    [Column("currentpage")]
    public int? CurrentPage { get; set; }

    [Column("totalpages")]
    public int? TotalPages { get; set; }

    [Column("updatedatutc")]
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
