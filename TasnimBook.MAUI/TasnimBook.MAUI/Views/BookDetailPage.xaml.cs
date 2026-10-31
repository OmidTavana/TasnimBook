using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly BookDetailViewModel _viewModel;
    public BookDetailPage(BookDetailViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; NavigationPage.SetHasNavigationBar(this, false); }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
}
