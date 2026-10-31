using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class AudioPlayerPage : ContentPage
{
    public AudioPlayerPage(AudioPlayerViewModel viewModel) { InitializeComponent(); BindingContext = viewModel; }
}
