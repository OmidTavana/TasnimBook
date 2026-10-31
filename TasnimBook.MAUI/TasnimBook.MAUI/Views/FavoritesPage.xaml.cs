using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _viewModel;
    public FavoritesPage(FavoritesViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
}
