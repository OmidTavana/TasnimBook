using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public partial class BookReaderPage : ContentPage
{
    private readonly Book _book;
    private readonly AccountService _account;
    private readonly string _localPdfPath;
    private ILocalPdfDocument? _document;
    private int _pageIndex;
    private bool _changingPage;

    public BookReaderPage(Book book, BooksDTO dto, AccountService account, string localPdfPath)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _book = book;
        _account = account;
        _localPdfPath = localPdfPath;
        ReaderTitle.Text = dto.Title;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_document is not null) return;
        try
        {
            _document = LocalPdfDocumentFactory.Create();
            await _document.OpenAsync(_localPdfPath);
            var progress = await _account.GetReadingProgressAsync(_book.Id);
            _pageIndex = progress.CurrentPage is > 0
                ? Math.Clamp(progress.CurrentPage.Value - 1, 0, Math.Max(0, _document.PageCount - 1))
                : Math.Clamp((int)Math.Floor((double)progress.PositionPercent / 100 * _document.PageCount), 0, Math.Max(0, _document.PageCount - 1));
            await ShowPageAsync();
        }
        catch (Exception ex)
        {
            PageLoading.IsRunning = false;
            await DisplayAlert("مطالعه PDF", ex.Message, "بازگشت");
            await Navigation.PopAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        await SaveProgressAsync();
        if (_document is not null)
        {
            await _document.DisposeAsync();
            _document = null;
        }
        base.OnDisappearing();
    }

    private async Task ShowPageAsync()
    {
        if (_document is null || _document.PageCount == 0 || _changingPage) return;
        _changingPage = true;
        PageLoading.IsVisible = true;
        PageLoading.IsRunning = true;
        PreviousButton.IsEnabled = false;
        NextButton.IsEnabled = false;
        try
        {
            var width = (int)Math.Max(720, Width * DeviceDisplay.Current.MainDisplayInfo.Density);
            var bytes = await _document.RenderPageAsync(_pageIndex, width);
            PdfPageImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            await PageScroll.ScrollToAsync(0, 0, false);
            var pageNumber = _pageIndex + 1;
            PageNumberLabel.Text = $"{pageNumber} از {_document.PageCount}";
            ReaderProgressText.Text = $"صفحه {pageNumber} از {_document.PageCount}";
            PageProgress.Progress = (double)pageNumber / _document.PageCount;
            PreviousButton.IsEnabled = _pageIndex > 0;
            NextButton.IsEnabled = _pageIndex < _document.PageCount - 1;
            await SaveProgressAsync();
        }
        finally
        {
            PageLoading.IsRunning = false;
            PageLoading.IsVisible = false;
            _changingPage = false;
        }
    }

    private async Task SaveProgressAsync()
    {
        if (_document is null || _document.PageCount == 0) return;
        var percent = Math.Clamp((decimal)(_pageIndex + 1) / _document.PageCount * 100, 0, 100);
        try { await _account.SaveProgressAsync(_book.Id, percent, _pageIndex + 1, _document.PageCount); }
        catch { }
    }

    private async void PreviousClicked(object sender, EventArgs e)
    {
        if (_pageIndex <= 0) return;
        _pageIndex--;
        await ShowPageAsync();
    }

    private async void NextClicked(object sender, EventArgs e)
    {
        if (_document is null || _pageIndex >= _document.PageCount - 1) return;
        _pageIndex++;
        await ShowPageAsync();
    }

    private async void BackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
