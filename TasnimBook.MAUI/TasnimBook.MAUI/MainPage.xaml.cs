using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;
using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI;

public partial class MainPage : ContentPage
{
    private readonly BookCatalogService _catalog;
    private readonly AccountService _account;
    private readonly BookAssetService _assets;

    public MainPage(BookCatalogService catalog, AccountService account, BookAssetService assets, HomeViewModel viewModel)
    {
        InitializeComponent();
        _catalog = catalog;
        _account = account;
        _assets = assets;
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm && vm.NewBooks.Count == 0) await vm.LoadAsync();
    }

    private async void BookSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView view || e.CurrentSelection.FirstOrDefault() is not BooksDTO dto) return;
        view.SelectedItem = null;
        var book = await _catalog.GetBookAsync(dto.Id);
        await Navigation.PushAsync(dto.IsAudioBook == true ? new AudioPlayerPage(book, dto) : new BookDetailPage(book, dto, _account, _assets));
    }

    private async void NewBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "کتاب‌های تازه", BookListMode.New));
    private async void SuggestedBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "کتاب‌های پیشنهادی", BookListMode.Suggested));
    private async void AudioBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "کتاب‌های صوتی", BookListMode.Audio));
    private async void SearchClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "جست‌وجوی کتاب", BookListMode.Search));
    private async void HomeTapped(object? sender, TappedEventArgs e) => await ((HomeViewModel)BindingContext).LoadAsync();
    private async void CatalogTapped(object? sender, TappedEventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "فهرست کتاب‌ها", BookListMode.New));
    private async void SearchTapped(object? sender, TappedEventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, _account, _assets, "جست‌وجوی کتاب", BookListMode.Search));
    private async void LibraryTapped(object? sender, TappedEventArgs e)
    {
        await _account.InitializeAsync();
        await Navigation.PushAsync(_account.IsLoggedIn ? new FavoritesPage(_account, _catalog, _assets) : new AccountPage(_account, _catalog, _assets));
    }
    private async void SettingsTapped(object? sender, TappedEventArgs e) => await Navigation.PushAsync(new AccountPage(_account, _catalog, _assets));
}
