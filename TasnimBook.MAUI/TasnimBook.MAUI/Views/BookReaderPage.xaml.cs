using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class BookReaderPage : ContentPage
{
    private readonly BookReaderViewModel _viewModel;
    public BookReaderPage(BookReaderViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; NavigationPage.SetHasNavigationBar(this, false); }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
    protected override async void OnDisappearing() { await _viewModel.OnDisappearingAsync(); base.OnDisappearing(); }
}
