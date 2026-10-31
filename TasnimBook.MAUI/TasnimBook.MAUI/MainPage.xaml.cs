using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI;

public partial class MainPage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    public MainPage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.NewBooks.Count == 0) await _viewModel.LoadAsync();
    }
}
