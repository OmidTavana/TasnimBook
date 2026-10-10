using TasnimBook.Data;

namespace TasnimBook.MAUI.Views;

internal static class BookUi
{
    internal static string Author(Book book) =>
        book.Author is null ? "نویسنده نامشخص" : book.Author.FullName;
}
