using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public enum BookListMode { New, Suggested, Audio, Search }

public partial class BookListPage : ContentPage
{
    private readonly BookCatalogService _catalog;
    private readonly BookListMode _mode;
    private bool _loaded;

    public BookListPage(BookCatalogService catalog, string title, BookListMode mode)
    {
        InitializeComponent();
        _catalog = catalog;
        _mode = mode;
        Title = title;
        BookSearchBar.IsVisible = mode == BookListMode.Search;
        EmptyLabel.Text = mode == BookListMode.Search ? "عبارت جست‌وجو را وارد کنید." : "کتابی برای نمایش نیست.";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded && _mode != BookListMode.Search) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IReadOnlyList<BooksDTO> books = _mode switch
        {
            BookListMode.New => await _catalog.GetNewAsync(40),
            BookListMode.Suggested => await _catalog.GetSuggestedAsync(40),
            BookListMode.Audio => await _catalog.GetAudioAsync(50),
            _ => await _catalog.SearchAsync(BookSearchBar.Text ?? string.Empty)
        };
        BooksView.ItemsSource = books;
        _loaded = true;
    }

    private async void SearchRequested(object sender, EventArgs e) => await LoadAsync();
    private async void SearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_mode == BookListMode.Search && e.NewTextValue?.Length >= 3) await LoadAsync();
    }

    private async void BookSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not BooksDTO dto) return;
        BooksView.SelectedItem = null;
        var book = await _catalog.GetBookAsync(dto.Id);
        await Navigation.PushAsync(dto.IsAudioBook == true ? new AudioPlayerPage(book, dto) : new BookDetailPage(book, dto));
    }
}
