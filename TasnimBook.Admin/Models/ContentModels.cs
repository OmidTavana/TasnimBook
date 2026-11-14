using System.ComponentModel.DataAnnotations;
using TasnimBook.Data;

namespace TasnimBook.Admin.Models;

public sealed class BookEditorModel
{
    public int Id { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Url] public string? Url { get; set; }
    [Url] public string? ImageUrl { get; set; }
    public string? FileSize { get; set; }
    public string? PagesCount { get; set; }
    public int? AuthorId { get; set; }
    public bool IsAudioBook { get; set; }
    public bool IsSuggested { get; set; }
    public string CategoryIds { get; set; } = string.Empty;

    public Book ToEntity() => new() { Id = Id, Title = Title, Description = Description ?? string.Empty, Url = Url ?? string.Empty, ImageUrl = ImageUrl ?? string.Empty, FileSize = FileSize ?? string.Empty, PagesCount = PagesCount ?? string.Empty, AuthorId = AuthorId, IsAudioBook = IsAudioBook, IsSuggested = IsSuggested };
    public int[] ParsedCategoryIds() => CategoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0).Distinct().ToArray();
}
