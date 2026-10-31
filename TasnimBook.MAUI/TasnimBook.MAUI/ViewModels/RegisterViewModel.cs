using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class RegisterViewModel : ObservableObject
{
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private string _displayName = string.Empty, _email = string.Empty, _password = string.Empty, _confirmPassword = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(ButtonText)); } }
    public string ButtonText => IsBusy ? "در حال ساخت حساب…" : "ثبت‌نام و ورود";
    public AsyncCommand RegisterCommand { get; }

    public RegisterViewModel(AccountService account, AppNavigationService navigation)
    {
        _account = account; _navigation = navigation;
        RegisterCommand = new AsyncCommand(RegisterAsync);
    }

    private async Task RegisterAsync()
    {
        ErrorMessage = null;
        if (Password != ConfirmPassword) { ErrorMessage = "تکرار رمز عبور یکسان نیست."; return; }
        IsBusy = true;
        try { await _account.RegisterAsync(DisplayName, Email, Password); await _navigation.GoBackAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
