using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public enum BookListMode { New, Suggested, Audio, Search }

public partial class BookListPage : ContentPage
{
    private readonly BookListViewModel _viewModel;
    public BookListPage(BookListViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
}
