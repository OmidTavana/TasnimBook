using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;
using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI;

public partial class MainPage : ContentPage
{
    private readonly BookCatalogService _catalog;

    public MainPage(BookCatalogService catalog, HomeViewModel viewModel)
    {
        InitializeComponent();
        _catalog = catalog;
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
        await Navigation.PushAsync(dto.IsAudioBook == true ? new AudioPlayerPage(book, dto) : new BookDetailPage(book, dto));
    }

    private async void NewBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, "کتاب‌های تازه", BookListMode.New));
    private async void SuggestedBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, "کتاب‌های پیشنهادی", BookListMode.Suggested));
    private async void AudioBooksClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, "کتاب‌های صوتی", BookListMode.Audio));
    private async void SearchClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BookListPage(_catalog, "جست‌وجوی کتاب", BookListMode.Search));
    private async void HomeClicked(object sender, EventArgs e) => await ((HomeViewModel)BindingContext).LoadAsync();
}
