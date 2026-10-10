using TasnimBook.Data;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Views;

public partial class AudioPlayerPage : ContentPage
{
    private readonly Book _book;
    private readonly BooksDTO _dto;
    private bool _isPlaying;

    public AudioPlayerPage(Book book, BooksDTO dto)
    {
        InitializeComponent();
        _book = book;
        _dto = dto;
        CoverImage.Source = dto.ImageUrl;
        TitleLabel.Text = dto.Title;
        AuthorLabel.Text = BookUi.Author(book);
    }

    private async void PlayClicked(object sender, EventArgs e)
    {
        _isPlaying = !_isPlaying;
        PlayButton.Text = _isPlaying ? "Ⅱ" : "▶";
        if (!_isPlaying) return;
        var url = string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url : _book.Url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await DisplayAlert("پخش", "فایل صوتی برای این عنوان موجود نیست.", "باشه");
            _isPlaying = false;
            PlayButton.Text = "▶";
            return;
        }
        await Launcher.Default.OpenAsync(uri);
    }
}
