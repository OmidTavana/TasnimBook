using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.ViewModels;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.Services;

public sealed class AppNavigationService
{
    private readonly BookCatalogService _catalog;
    private readonly AccountService _account;
    private readonly BookAssetService _assets;

    public AppNavigationService(BookCatalogService catalog, AccountService account, BookAssetService assets)
    {
        _catalog = catalog;
        _account = account;
        _assets = assets;
    }

    private INavigation Navigation => Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("Navigation is not ready.");
    private Page CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException("The current page is not ready.");

    public async Task OpenBookAsync(BooksDTO dto)
    {
        var book = await _catalog.GetBookAsync(dto.Id);
        if (dto.IsAudioBook == true)
            await Navigation.PushAsync(new AudioPlayerPage(new AudioPlayerViewModel(book, dto, this)));
        else
            await Navigation.PushAsync(new BookDetailPage(new BookDetailViewModel(book, dto, _account, _assets, this)));
    }

    public Task OpenBookListAsync(string title, BookListMode mode) =>
        Navigation.PushAsync(new BookListPage(new BookListViewModel(_catalog, title, mode, this)));

    public Task OpenAccountAsync(bool favoritesAvailable = true) =>
        Navigation.PushAsync(new AccountPage(new AccountViewModel(_account, this, favoritesAvailable)));

    public Task OpenRegisterAsync() => Navigation.PushAsync(new RegisterPage(new RegisterViewModel(_account, this)));
    public Task OpenFavoritesAsync() => Navigation.PushAsync(new FavoritesPage(new FavoritesViewModel(_account, this)));
    public Task OpenReaderAsync(Book book, BooksDTO dto, string localPath) =>
        Navigation.PushAsync(new BookReaderPage(new BookReaderViewModel(book, dto, localPath, _account, this)));
    public Task GoBackAsync() => Navigation.PopAsync();

    public Task AlertAsync(string title, string message, string cancel = "باشه") =>
        CurrentPage.DisplayAlertAsync(title, message, cancel);
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel);
    public Task ShareAsync(string title, string text, string uri) => Share.Default.RequestAsync(new ShareTextRequest { Title = title, Text = text, Uri = uri });
    public Task OpenExternalAsync(Uri uri) => Launcher.Default.OpenAsync(uri);
}
