using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class AccountViewModel : ObservableObject
{
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isLoggedIn;
    private string _welcomeText = string.Empty;
    private string _profileEmail = string.Empty;

    public bool FavoritesAvailable { get; }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(LoginButtonText)); } }
    public bool IsLoggedIn { get => _isLoggedIn; private set { if (SetProperty(ref _isLoggedIn, value)) OnPropertyChanged(nameof(IsLoggedOut)); } }
    public bool IsLoggedOut => !IsLoggedIn;
    public string LoginButtonText => IsBusy ? "در حال ورود…" : "ورود امن";
    public string WelcomeText { get => _welcomeText; private set => SetProperty(ref _welcomeText, value); }
    public string ProfileEmail { get => _profileEmail; private set => SetProperty(ref _profileEmail, value); }
    public AsyncCommand LoginCommand { get; }
    public AsyncCommand RegisterCommand { get; }
    public AsyncCommand FavoritesCommand { get; }
    public AsyncCommand LogoutCommand { get; }

    public AccountViewModel(AccountService account, AppNavigationService navigation, bool favoritesAvailable)
    {
        _account = account;
        _navigation = navigation;
        FavoritesAvailable = favoritesAvailable;
        LoginCommand = new AsyncCommand(LoginAsync);
        RegisterCommand = new AsyncCommand(_navigation.OpenRegisterAsync);
        FavoritesCommand = new AsyncCommand(_navigation.OpenFavoritesAsync);
        LogoutCommand = new AsyncCommand(LogoutAsync);
    }

    public async Task OnAppearingAsync() { await _account.InitializeAsync(); RefreshState(); }

    private async Task LoginAsync()
    {
        IsBusy = true; ErrorMessage = null;
        try { await _account.LoginAsync(Email, Password); Password = string.Empty; RefreshState(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task LogoutAsync() { await _account.LogoutAsync(); RefreshState(); }
    private void RefreshState()
    {
        IsLoggedIn = _account.IsLoggedIn;
        WelcomeText = _account.CurrentUser is null ? string.Empty : $"سلام {_account.CurrentUser.DisplayName}";
        ProfileEmail = _account.CurrentUser?.Email ?? string.Empty;
    }
}
