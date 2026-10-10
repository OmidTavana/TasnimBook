using TasnimBook.Data;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly Book _book;
    private readonly BooksDTO _dto;

    public BookDetailPage(Book book, BooksDTO dto)
    {
        InitializeComponent();
        _book = book;
        _dto = dto;
        CoverImage.Source = dto.ImageUrl;
        TitleLabel.Text = dto.Title;
        AuthorLabel.Text = BookUi.Author(book);
        MetadataLabel.Text = $"{dto.PagesCount ?? "—"} صفحه    •    {dto.FileSize ?? "حجم نامشخص"}";
        DescriptionLabel.Text = string.IsNullOrWhiteSpace(book.Description) ? dto.Description ?? "توضیحی برای این کتاب ثبت نشده است." : book.Description;
    }

    private async void DownloadClicked(object sender, EventArgs e)
    {
        var url = string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url : _book.Url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) { await DisplayAlert("دانلود", "لینک دانلود برای این کتاب موجود نیست.", "باشه"); return; }
        await Launcher.Default.OpenAsync(uri);
    }
}
