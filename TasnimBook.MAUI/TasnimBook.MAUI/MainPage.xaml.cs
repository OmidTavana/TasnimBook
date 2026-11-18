using TasnimBook.MAUI.ViewModels;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI;

public partial class MainPage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly AppUpdateService _appUpdate;
    private readonly AppNotificationService _notifications;
    public MainPage(
        HomeViewModel viewModel,
        AppUpdateService appUpdate,
        AppNotificationService notifications)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _appUpdate = appUpdate;
        _notifications = notifications;
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAccountStateAsync();
        if (_viewModel.NewBooks.Count == 0) await _viewModel.LoadAsync();
        await _notifications.CheckAsync();
        await _appUpdate.CheckForUpdateAsync();
    }
}
