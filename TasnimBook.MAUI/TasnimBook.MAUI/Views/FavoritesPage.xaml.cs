using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly AccountService _account;
    private readonly BookCatalogService _catalog;
    private readonly BookAssetService _assets;
    public FavoritesPage(AccountService account, BookCatalogService catalog, BookAssetService assets) { InitializeComponent(); _account = account; _catalog = catalog; _assets = assets; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Loading.IsVisible = true;
        try { BooksView.ItemsSource = await _account.GetFavoritesAsync(); }
        catch (Exception ex) { await DisplayAlert("علاقه‌مندی‌ها", ex.Message, "باشه"); }
        finally { Loading.IsVisible = false; Loading.IsRunning = false; }
    }

    private async void BookSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not BooksDTO dto) return;
        BooksView.SelectedItem = null;
        var book = await _catalog.GetBookAsync(dto.Id);
        await Navigation.PushAsync(dto.IsAudioBook == true ? new AudioPlayerPage(book, dto) : new BookDetailPage(book, dto, _account, _assets));
    }
}
