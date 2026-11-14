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

    public string CategoryIds { get; set; } = string.Empty;

    public static BookEditorModel FromEntity(Book book)
    {
        var categoryIds = book.BookCategories
            .Select(relation => relation.BookCategoryId);

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
            CategoryIds = string.Join(',', categoryIds)
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

    public int[] ParseCategoryIds()
    {
        var categoryIds = new HashSet<int>();
        var values = CategoryIds.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var value in values)
        {
            if (int.TryParse(value, out var categoryId) && categoryId > 0)
            {
                categoryIds.Add(categoryId);
            }
        }

        return categoryIds.ToArray();
    }
}
