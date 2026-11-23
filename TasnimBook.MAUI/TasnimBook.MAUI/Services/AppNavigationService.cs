using Microsoft.Extensions.DependencyInjection;
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
    private readonly InAppMessageService _messages;
    private readonly IServiceProvider _services;

    public AppNavigationService(
        BookCatalogService catalog,
        AccountService account,
        BookAssetService assets,
        InAppMessageService messages,
        IServiceProvider services)
    {
        _catalog = catalog;
        _account = account;
        _assets = assets;
        _messages = messages;
        _services = services;
    }

    private INavigation Navigation => Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("Navigation is not ready.");
    private Page CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException("The current page is not ready.");

    public async Task OpenBookAsync(BooksDTO dto)
    {
        var book = await _catalog.GetBookAsync(dto.Id);
        await Navigation.PushAsync(new BookDetailPage(new BookDetailViewModel(book, dto, _account, _assets, this)));
    }

    public Task OpenBookListAsync(string title, BookListMode mode, int? categoryId = null) =>
        Navigation.PushAsync(new BookListPage(new BookListViewModel(_catalog, title, mode, this, categoryId)));

    public Task OpenCategoriesAsync() =>
        Navigation.PushAsync(new CategoriesPage(new CategoriesViewModel(
            _catalog,
            _services.GetRequiredService<CategoryCoverCacheService>(),
            this)));

    public Task OpenCategoryBooksAsync(CategoryCardItem category) =>
        OpenBookListAsync(category.CategoryName, BookListMode.Category, category.Id);

    public Task OpenAccountAsync(bool favoritesAvailable = true) =>
        Navigation.PushAsync(new AccountPage(new AccountViewModel(_account, this, favoritesAvailable)));

    public Task OpenSettingsAsync() =>
        Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    public Task OpenRegisterAsync() => Navigation.PushAsync(new RegisterPage(new RegisterViewModel(_account, this)));
    public Task OpenFavoritesAsync() => Navigation.PushAsync(new FavoritesPage(new FavoritesViewModel(_account, this)));
    public Task OpenLibraryAsync() => Navigation.PushAsync(new LibraryPage(new LibraryViewModel(_account, this)));
    public Task OpenReaderAsync(Book book, BooksDTO dto, string localPath) =>
        Navigation.PushAsync(new BookReaderPage(new BookReaderViewModel(book, dto, localPath, _account, this)));
    public Task GoBackAsync() => Navigation.PopAsync();

    public Task AlertAsync(
        string title,
        string message,
        string cancel = "باشه",
        AppMessageType type = AppMessageType.Information) =>
        _messages.ShowDialogAsync(title, message, cancel, type);

    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string accept,
        string cancel,
        AppMessageType type = AppMessageType.Warning) =>
        _messages.ConfirmAsync(title, message, accept, cancel, type);

    public Task<bool> AcknowledgeAsync(
        string title,
        string message,
        string accept = "باشه",
        AppMessageType type = AppMessageType.Information) =>
        _messages.ShowAcknowledgementAsync(title, message, accept, type);

    public Task ToastAsync(
        string message,
        AppMessageType type = AppMessageType.Information,
        TimeSpan? duration = null) =>
        _messages.ShowToastAsync(message, type, duration);
    public Task ShareAsync(string title, string text, string uri) => Share.Default.RequestAsync(new ShareTextRequest { Title = title, Text = text, Uri = uri });
    public Task OpenExternalAsync(Uri uri) => Launcher.Default.OpenAsync(uri);
}
