using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.ViewModels;

public sealed class AudioPlayerViewModel : ObservableObject
{
    private readonly Book _book;
    private readonly BooksDTO _dto;
    private readonly AppNavigationService _navigation;
    private bool _isPlaying;
    public string Title => _dto.Title;
    public string Author => BookUi.Author(_book);
    public string Cover => string.IsNullOrWhiteSpace(_dto.ImageUrl) ? "book_cover_placeholder.svg" : _dto.ImageUrl;
    public string PlayIcon => _isPlaying ? "icon_pause.svg" : "icon_play.svg";
    public AsyncCommand PlayCommand { get; }

    public AudioPlayerViewModel(Book book, BooksDTO dto, AppNavigationService navigation)
    {
        _book = book; _dto = dto; _navigation = navigation;
        PlayCommand = new AsyncCommand(PlayAsync);
    }

    private async Task PlayAsync()
    {
        _isPlaying = !_isPlaying; OnPropertyChanged(nameof(PlayIcon));
        if (!_isPlaying) return;
        var url = string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url : _book.Url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await _navigation.AlertAsync("پخش", "فایل صوتی برای این عنوان موجود نیست.");
            _isPlaying = false; OnPropertyChanged(nameof(PlayIcon)); return;
        }
        await _navigation.OpenExternalAsync(uri);
    }
}
