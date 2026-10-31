using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AccountService _account;
    public RegisterPage(AccountService account) { InitializeComponent(); _account = account; }

    private async void RegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (PasswordEntry.Text != ConfirmEntry.Text)
        {
            ErrorLabel.Text = "تکرار رمز عبور یکسان نیست."; ErrorLabel.IsVisible = true; return;
        }
        RegisterButton.IsEnabled = false; RegisterButton.Text = "در حال ساخت حساب…";
        try
        {
            await _account.RegisterAsync(NameEntry.Text ?? string.Empty, EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
            await Navigation.PopAsync();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; ErrorLabel.IsVisible = true; }
        finally { RegisterButton.IsEnabled = true; RegisterButton.Text = "ثبت‌نام و ورود"; }
    }
}
