using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public partial class AccountPage : ContentPage
{
    private readonly AccountService _account;
    private readonly BookCatalogService? _catalog;
    private readonly BookAssetService _assets;

    public AccountPage(AccountService account, BookCatalogService? catalog, BookAssetService assets)
    {
        InitializeComponent();
        _account = account;
        _catalog = catalog;
        _assets = assets;
        FavoritesButton.IsVisible = catalog is not null;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _account.InitializeAsync();
        RefreshState();
    }

    private void RefreshState()
    {
        LoginPanel.IsVisible = !_account.IsLoggedIn;
        ProfilePanel.IsVisible = _account.IsLoggedIn;
        if (_account.CurrentUser is not null)
        {
            WelcomeLabel.Text = $"سلام {_account.CurrentUser.DisplayName}";
            ProfileEmailLabel.Text = _account.CurrentUser.Email;
        }
    }

    private async void LoginClicked(object sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _account.LoginAsync(EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
            ErrorLabel.IsVisible = false;
            RefreshState();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; ErrorLabel.IsVisible = true; }
        finally { SetBusy(false); }
    }

    private async void RegisterClicked(object sender, EventArgs e) => await Navigation.PushAsync(new RegisterPage(_account));
    private async void FavoritesClicked(object sender, EventArgs e)
    {
        if (_catalog is not null) await Navigation.PushAsync(new FavoritesPage(_account, _catalog, _assets));
    }
    private async void LogoutClicked(object sender, EventArgs e)
    {
        await _account.LogoutAsync();
        RefreshState();
    }
    private void SetBusy(bool value) { LoginButton.IsEnabled = !value; LoginButton.Text = value ? "در حال ورود…" : "ورود امن"; }
}
