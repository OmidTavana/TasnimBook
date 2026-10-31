using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel) { InitializeComponent(); BindingContext = viewModel; }
}
