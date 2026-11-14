using System.ComponentModel.DataAnnotations;
using TasnimBook.Data;

namespace TasnimBook.Admin.Models;

public sealed class BookEditorModel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Url]
    public string? Url { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    public string? FileSize { get; set; }

    public string? PagesCount { get; set; }

    public int? AuthorId { get; set; }

    public bool IsAudioBook { get; set; }

    public bool IsSuggested { get; set; }

    public int[] CategoryIds { get; set; } = [];

    public IFormFile? CoverImage { get; set; }

    public IFormFile? BookFile { get; set; }

    public string? CoverPreviewUrl { get; set; }

    public IReadOnlyList<Author> AvailableAuthors { get; set; } = [];

    public IReadOnlyList<BookCat> AvailableCategories { get; set; } = [];

    public static BookEditorModel FromEntity(Book book)
    {
        var categoryIds = book.BookCategories?
            .Select(relation => relation.BookCategoryId)
            .ToArray() ?? [];

        return new BookEditorModel
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            Url = book.Url,
            ImageUrl = book.ImageUrl,
            FileSize = book.FileSize,
            PagesCount = book.PagesCount,
            AuthorId = book.AuthorId,
            IsAudioBook = book.IsAudioBook == true,
            IsSuggested = book.IsSuggested == true,
            CategoryIds = categoryIds
        };
    }

    public Book ToEntity()
    {
        return new Book
        {
            Id = Id,
            Title = Title,
            Description = Description ?? string.Empty,
            Url = Url ?? string.Empty,
            ImageUrl = ImageUrl ?? string.Empty,
            FileSize = FileSize ?? string.Empty,
            PagesCount = PagesCount ?? string.Empty,
            AuthorId = AuthorId,
            IsAudioBook = IsAudioBook,
            IsSuggested = IsSuggested
        };
    }

}
