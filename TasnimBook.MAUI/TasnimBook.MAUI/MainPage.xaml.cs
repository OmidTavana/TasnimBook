using TasnimBook.MAUI.ViewModels;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI;

public partial class MainPage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly AppUpdateService _appUpdate;
    public MainPage(HomeViewModel viewModel, AppUpdateService appUpdate)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _appUpdate = appUpdate;
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.NewBooks.Count == 0) await _viewModel.LoadAsync();
        await _appUpdate.CheckForUpdateAsync();
    }
}
