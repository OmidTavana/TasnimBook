using TasnimBook.MAUI.ViewModels;
using TasnimBook.Data.Models;

namespace TasnimBook.MAUI.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _viewModel;
    public LibraryPage(LibraryViewModel viewModel) { InitializeComponent(); BindingContext = _viewModel = viewModel; }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
    private async void OnOpenClicked(object sender, EventArgs e) { if (sender is Button { CommandParameter: BooksDTO book }) await _viewModel.OpenAsync(book); }
    private async void OnRemoveClicked(object sender, EventArgs e) { if (sender is Button { CommandParameter: BooksDTO book }) await _viewModel.RemoveAsync(book); }
}
