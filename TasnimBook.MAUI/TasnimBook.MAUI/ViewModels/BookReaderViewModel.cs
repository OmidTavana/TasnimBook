using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class BookReaderViewModel : ObservableObject
{
    private readonly Book _book;
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private CancellationTokenSource? _saveDebounce;
    private int _pageIndex;
    private int _pageCount;
    private int? _restoredPageIndex;
    private decimal _restoredPercent;
    private bool _progressRestored;
    private bool _isBusy = true;
    private double _zoom = 1;
    private string _zoomText = "۱۰۰٪";
    private string _pageText = "در حال آماده‌سازی…";
    private string _pageCounterText = "— / —";
    private string _pageNumberInput = string.Empty;
    private double _pageProgress;
    private string? _readerError;

    public string Title { get; }
    public string LocalPath { get; }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public int CurrentPageIndex
    {
        get => _pageIndex;
        set
        {
            var normalized = Math.Max(0, value);
            if (!SetProperty(ref _pageIndex, normalized)) return;
            UpdatePageState();
            if (_progressRestored && _pageCount > 0) ScheduleProgressSave();
            UpdateCommands();
        }
    }
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, Math.Clamp(value, 0.75, 3)))
                ZoomText = $"{Math.Round(_zoom * 100):0}٪";
        }
    }
    public string ZoomText { get => _zoomText; private set => SetProperty(ref _zoomText, value); }
    public string PageText { get => _pageText; private set => SetProperty(ref _pageText, value); }
    public string PageCounterText { get => _pageCounterText; private set => SetProperty(ref _pageCounterText, value); }
    public string PageNumberInput { get => _pageNumberInput; set => SetProperty(ref _pageNumberInput, value); }
    public double PageProgress { get => _pageProgress; private set => SetProperty(ref _pageProgress, value); }
    public string? ReaderError
    {
        get => _readerError;
        private set
        {
            if (SetProperty(ref _readerError, value)) OnPropertyChanged(nameof(HasReaderError));
        }
    }
    public bool HasReaderError => !string.IsNullOrWhiteSpace(ReaderError);
    public string TotalPagesText => _pageCount == 0 ? "—" : _pageCount.ToString();

    public AsyncCommand BackCommand { get; }
    public AsyncCommand PreviousCommand { get; }
    public AsyncCommand NextCommand { get; }
    public AsyncCommand FirstCommand { get; }
    public AsyncCommand LastCommand { get; }
    public AsyncCommand GoToPageCommand { get; }
    public AsyncCommand ZoomInCommand { get; }
    public AsyncCommand ZoomOutCommand { get; }
    public AsyncCommand ResetZoomCommand { get; }

    public BookReaderViewModel(Book book, BooksDTO dto, string localPath, AccountService account, AppNavigationService navigation)
    {
        _book = book;
        _account = account;
        _navigation = navigation;
        Title = dto.Title;
        LocalPath = localPath;

        BackCommand = new AsyncCommand(_navigation.GoBackAsync);
        PreviousCommand = new AsyncCommand(GoToPreviousPageAsync, () => !IsBusy && CurrentPageIndex > 0);
        NextCommand = new AsyncCommand(GoToNextPageAsync, () => !IsBusy && CurrentPageIndex < _pageCount - 1);
        FirstCommand = new AsyncCommand(GoToFirstPageAsync, () => !IsBusy && CurrentPageIndex > 0);
        LastCommand = new AsyncCommand(GoToLastPageAsync, () => !IsBusy && CurrentPageIndex < _pageCount - 1);
        GoToPageCommand = new AsyncCommand(GoToPageAsync, () => !IsBusy && _pageCount > 0);
        ZoomInCommand = new AsyncCommand(() => { SetZoom(Zoom + 0.25); return Task.CompletedTask; });
        ZoomOutCommand = new AsyncCommand(() => { SetZoom(Zoom - 0.25); return Task.CompletedTask; });
        ResetZoomCommand = new AsyncCommand(() => { SetZoom(1); return Task.CompletedTask; });
    }

    public async Task OnAppearingAsync()
    {
        if (_progressRestored) return;
        try
        {
            var progress = await _account.GetReadingProgressAsync(_book.Id);
            _restoredPageIndex = progress.CurrentPage is > 0 ? progress.CurrentPage.Value - 1 : null;
            _restoredPercent = progress.PositionPercent;
        }
        catch { }
        finally
        {
            _progressRestored = true;
            RestorePageIfReady();
        }
    }

    public async Task OnDisappearingAsync()
    {
        _saveDebounce?.Cancel();
        _saveDebounce?.Dispose();
        _saveDebounce = null;
        await SaveProgressAsync();
    }

    public void OnDocumentLoaded(int pageCount)
    {
        _pageCount = Math.Max(0, pageCount);
        IsBusy = false;
        ReaderError = null;
        OnPropertyChanged(nameof(TotalPagesText));
        RestorePageIfReady();
        UpdatePageState();
        UpdateCommands();
    }

    public void OnPageChanged(int pageIndex, int pageCount)
    {
        if (pageCount > 0 && pageCount != _pageCount)
        {
            _pageCount = pageCount;
            OnPropertyChanged(nameof(TotalPagesText));
        }
        CurrentPageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, _pageCount - 1));
    }

    public void OnReaderError(string message)
    {
        IsBusy = false;
        ReaderError = string.IsNullOrWhiteSpace(message) ? "نمایش PDF انجام نشد. دوباره تلاش کنید." : message;
    }

    public Task GoToFirstPageAsync() { CurrentPageIndex = 0; return Task.CompletedTask; }
    public Task GoToLastPageAsync() { CurrentPageIndex = Math.Max(0, _pageCount - 1); return Task.CompletedTask; }
    public Task GoToPreviousPageAsync() { CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1); return Task.CompletedTask; }
    public Task GoToNextPageAsync() { CurrentPageIndex = Math.Min(Math.Max(0, _pageCount - 1), CurrentPageIndex + 1); return Task.CompletedTask; }

    public Task GoToPageAsync()
    {
        var normalized = NormalizeDigits(PageNumberInput);
        if (int.TryParse(normalized, out var page) && page >= 1 && page <= _pageCount)
        {
            ReaderError = null;
            CurrentPageIndex = page - 1;
        }
        else
        {
            ReaderError = $"شماره صفحه باید بین ۱ و {_pageCount} باشد.";
        }
        return Task.CompletedTask;
    }

    public void SetZoom(double value) => Zoom = Math.Clamp(value, 0.75, 3);

    private void RestorePageIfReady()
    {
        if (!_progressRestored || _pageCount == 0) return;
        var restored = _restoredPageIndex ?? (int)Math.Floor((double)_restoredPercent / 100 * _pageCount);
        CurrentPageIndex = Math.Clamp(restored, 0, Math.Max(0, _pageCount - 1));
        _restoredPageIndex = null;
        _restoredPercent = 0;
    }

    private void UpdatePageState()
    {
        if (_pageCount == 0)
        {
            PageText = "در حال آماده‌سازی…";
            PageCounterText = "— / —";
            PageProgress = 0;
            return;
        }

        var number = Math.Clamp(CurrentPageIndex + 1, 1, _pageCount);
        PageText = $"صفحه {number} از {_pageCount}";
        PageCounterText = $"{number} / {_pageCount}";
        PageNumberInput = number.ToString();
        PageProgress = (double)number / _pageCount;
    }

    private void ScheduleProgressSave()
    {
        _saveDebounce?.Cancel();
        _saveDebounce?.Dispose();
        var debounce = _saveDebounce = new CancellationTokenSource();
        _ = SaveAfterDelayAsync(debounce);
    }

    private async Task SaveAfterDelayAsync(CancellationTokenSource debounce)
    {
        try
        {
            await Task.Delay(500, debounce.Token);
            await SaveProgressAsync();
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_saveDebounce, debounce)) _saveDebounce = null;
            debounce.Dispose();
        }
    }

    private async Task SaveProgressAsync()
    {
        if (!_progressRestored || _pageCount == 0) return;
        var percent = Math.Clamp((decimal)(CurrentPageIndex + 1) / _pageCount * 100, 0, 100);
        try { await _account.SaveProgressAsync(_book.Id, percent, CurrentPageIndex + 1, _pageCount); }
        catch { }
    }

    private static string NormalizeDigits(string value)
    {
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        return new string(value.Select(character =>
        {
            var index = persian.IndexOf(character);
            if (index < 0) index = arabic.IndexOf(character);
            return index >= 0 ? (char)('0' + index) : character;
        }).ToArray());
    }

    private void UpdateCommands()
    {
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        FirstCommand.NotifyCanExecuteChanged();
        LastCommand.NotifyCanExecuteChanged();
        GoToPageCommand.NotifyCanExecuteChanged();
    }
}
