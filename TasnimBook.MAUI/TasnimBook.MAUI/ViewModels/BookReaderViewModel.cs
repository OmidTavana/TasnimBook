using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class BookReaderViewModel : ObservableObject
{
    private readonly Book _book;
    private readonly string _localPath;
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private ILocalPdfDocument? _document;
    private int _pageIndex;
    private bool _isBusy;
    private ImageSource? _pageImage;
    private string _pageText = "در حال آماده‌سازی…";
    private double _pageProgress;

    public string Title { get; }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public ImageSource? PageImage { get => _pageImage; private set => SetProperty(ref _pageImage, value); }
    public string PageText { get => _pageText; private set => SetProperty(ref _pageText, value); }
    public double PageProgress { get => _pageProgress; private set => SetProperty(ref _pageProgress, value); }
    public AsyncCommand BackCommand { get; }
    public AsyncCommand PreviousCommand { get; }
    public AsyncCommand NextCommand { get; }

    public BookReaderViewModel(Book book, BooksDTO dto, string localPath, AccountService account, AppNavigationService navigation)
    {
        _book = book; _localPath = localPath; _account = account; _navigation = navigation; Title = dto.Title;
        BackCommand = new AsyncCommand(_navigation.GoBackAsync);
        PreviousCommand = new AsyncCommand(PreviousAsync, () => !IsBusy && _pageIndex > 0);
        NextCommand = new AsyncCommand(NextAsync, () => !IsBusy && _document is not null && _pageIndex < _document.PageCount - 1);
    }

    public async Task OnAppearingAsync()
    {
        if (_document is not null) return;
        try
        {
            _document = LocalPdfDocumentFactory.Create();
            await _document.OpenAsync(_localPath);
            var progress = await _account.GetReadingProgressAsync(_book.Id);
            _pageIndex = progress.CurrentPage is > 0
                ? Math.Clamp(progress.CurrentPage.Value - 1, 0, Math.Max(0, _document.PageCount - 1))
                : Math.Clamp((int)Math.Floor((double)progress.PositionPercent / 100 * _document.PageCount), 0, Math.Max(0, _document.PageCount - 1));
            await ShowPageAsync();
        }
        catch (Exception ex)
        {
            await _navigation.AlertAsync("مطالعه PDF", ex.Message, "بازگشت");
            await _navigation.GoBackAsync();
        }
    }

    public async Task OnDisappearingAsync()
    {
        await SaveProgressAsync();
        if (_document is not null) { await _document.DisposeAsync(); _document = null; }
    }

    private async Task ShowPageAsync()
    {
        if (_document is null || _document.PageCount == 0 || IsBusy) return;
        IsBusy = true; UpdateCommands();
        try
        {
            var width = (int)Math.Max(720, DeviceDisplay.Current.MainDisplayInfo.Width);
            var bytes = await _document.RenderPageAsync(_pageIndex, width);
            PageImage = ImageSource.FromStream(() => new MemoryStream(bytes));
            var number = _pageIndex + 1;
            PageText = $"صفحه {number} از {_document.PageCount}";
            PageProgress = (double)number / _document.PageCount;
            await SaveProgressAsync();
        }
        finally { IsBusy = false; UpdateCommands(); }
    }

    private async Task SaveProgressAsync()
    {
        if (_document is null || _document.PageCount == 0) return;
        var percent = Math.Clamp((decimal)(_pageIndex + 1) / _document.PageCount * 100, 0, 100);
        try { await _account.SaveProgressAsync(_book.Id, percent, _pageIndex + 1, _document.PageCount); } catch { }
    }

    private async Task PreviousAsync() { if (_pageIndex > 0) { _pageIndex--; await ShowPageAsync(); } }
    private async Task NextAsync() { if (_document is not null && _pageIndex < _document.PageCount - 1) { _pageIndex++; await ShowPageAsync(); } }
    private void UpdateCommands() { PreviousCommand.NotifyCanExecuteChanged(); NextCommand.NotifyCanExecuteChanged(); }
}
