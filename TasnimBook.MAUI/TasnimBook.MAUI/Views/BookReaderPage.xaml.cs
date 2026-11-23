using TasnimBook.MAUI.Controls;
using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class BookReaderPage : ContentPage
{
    private readonly BookReaderViewModel _viewModel;
    public BookReaderPage(BookReaderViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; NavigationPage.SetHasNavigationBar(this, false); }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
    protected override async void OnDisappearing() { await _viewModel.OnDisappearingAsync(); base.OnDisappearing(); }

    private void OnDocumentLoaded(object? sender, PdfDocumentLoadedEventArgs e) => _viewModel.OnDocumentLoaded(e.PageCount);
    private void OnPageChanged(object? sender, PdfPageChangedEventArgs e) => _viewModel.OnPageChanged(e.PageIndex, e.PageCount);
    private void OnReaderError(object? sender, string message) => _viewModel.OnReaderError(message);
}
