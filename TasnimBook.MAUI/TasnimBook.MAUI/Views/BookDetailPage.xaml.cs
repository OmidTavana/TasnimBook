using TasnimBook.MAUI.ViewModels;

namespace TasnimBook.MAUI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly BookDetailViewModel _viewModel;
    public BookDetailPage(BookDetailViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; BindingContext = viewModel; NavigationPage.SetHasNavigationBar(this, false); }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.OnAppearingAsync(); }
    protected override async void OnDisappearing() { await _viewModel.OnDisappearingAsync(); base.OnDisappearing(); }
    private void OnAudioSeekStarted(object? sender, EventArgs e) => _viewModel.AudioPlayer?.BeginSeek();
    private async void OnAudioSeekCompleted(object? sender, EventArgs e)
    {
        if (_viewModel.AudioPlayer is not null) await _viewModel.AudioPlayer.CommitSeekAsync(AudioProgressSlider.Value);
    }
}
