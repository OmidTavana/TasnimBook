using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.ViewModels;

public sealed class BookDetailViewModel : ObservableObject
{
    private readonly Book _book;
    private readonly BooksDTO _dto;
    private readonly AccountService _account;
    private readonly BookAssetService _assets;
    private readonly AppNavigationService _navigation;
    private ImageSource _coverImage = ImageSource.FromFile("book_cover_placeholder.svg");
    private bool _isFavorite, _isDownloading, _isDownloadPanelVisible;
    private string _progressText = "هنوز شروع نشده", _readButtonText = "شروع مطالعه در برنامه";
    private string _downloadStatus = "در حال دریافت PDF…", _downloadPercentText = "۰٪", _downloadButtonText = "دریافت";
    private double _downloadProgress;

    public string Title => _dto.Title;
    public string Author => BookUi.Author(_book);
    public string Pages => _dto.PagesCount ?? "—";
    public string Size => _dto.FileSize ?? "—";
    public string Score => _book.Score is > 0 ? $"{_book.Score}/۵" : "—";
    public string Description => string.IsNullOrWhiteSpace(_book.Description) ? _dto.Description ?? "توضیحی برای این کتاب ثبت نشده است." : _book.Description;
    public ImageSource CoverImage { get => _coverImage; private set => SetProperty(ref _coverImage, value); }
    public string ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }
    public string ReadButtonText { get => _readButtonText; private set => SetProperty(ref _readButtonText, value); }
    public bool IsFavorite { get => _isFavorite; private set { if (SetProperty(ref _isFavorite, value)) { OnPropertyChanged(nameof(FavoriteIcon)); OnPropertyChanged(nameof(HeaderFavoriteIcon)); OnPropertyChanged(nameof(FavoriteText)); } } }
    public string FavoriteIcon => IsFavorite ? "icon_heart_filled.svg" : "icon_heart.svg";
    public string HeaderFavoriteIcon => IsFavorite ? "icon_heart_header_filled.svg" : "icon_heart_header.svg";
    public string FavoriteText => IsFavorite ? "ذخیره شده" : "علاقه‌مندی";
    public bool IsDownloading { get => _isDownloading; private set => SetProperty(ref _isDownloading, value); }
    public bool IsDownloadPanelVisible { get => _isDownloadPanelVisible; private set => SetProperty(ref _isDownloadPanelVisible, value); }
    public string DownloadStatus { get => _downloadStatus; private set => SetProperty(ref _downloadStatus, value); }
    public string DownloadPercentText { get => _downloadPercentText; private set => SetProperty(ref _downloadPercentText, value); }
    public string DownloadButtonText { get => _downloadButtonText; private set => SetProperty(ref _downloadButtonText, value); }
    public double DownloadProgress { get => _downloadProgress; private set => SetProperty(ref _downloadProgress, value); }
    public AsyncCommand BackCommand { get; }
    public AsyncCommand ReadCommand { get; }
    public AsyncCommand DownloadCommand { get; }
    public AsyncCommand FavoriteCommand { get; }
    public AsyncCommand ShareCommand { get; }

    public BookDetailViewModel(Book book, BooksDTO dto, AccountService account, BookAssetService assets, AppNavigationService navigation)
    {
        _book = book; _dto = dto; _account = account; _assets = assets; _navigation = navigation;
        BackCommand = new AsyncCommand(_navigation.GoBackAsync);
        ReadCommand = new AsyncCommand(ReadAsync, () => !IsDownloading);
        DownloadCommand = new AsyncCommand(DownloadAsync, () => !IsDownloading);
        FavoriteCommand = new AsyncCommand(ToggleFavoriteAsync);
        ShareCommand = new AsyncCommand(() => _navigation.ShareAsync(Title, $"{Title}\n{Author}", BookUrl));
    }

    public async Task OnAppearingAsync()
    {
        var imageUrl = string.IsNullOrWhiteSpace(_dto.ImageUrl) ? _book.ImageUrl : _dto.ImageUrl;
        var coverPath = await _assets.GetCoverPathAsync(_book.Id, imageUrl);
        CoverImage = string.IsNullOrWhiteSpace(coverPath) ? ImageSource.FromFile("book_cover_placeholder.svg") : ImageSource.FromFile(coverPath);
        try
        {
            var progress = await _account.GetProgressAsync(_book.Id);
            ProgressText = progress > 0 ? $"{progress:0}% مطالعه شده" : "هنوز شروع نشده";
            ReadButtonText = progress > 0 ? "ادامه مطالعه" : "شروع مطالعه در برنامه";
            IsFavorite = await _account.IsFavoriteAsync(_book.Id);
        }
        catch { }
    }

    private async Task ReadAsync()
    {
        var path = await EnsureDownloadedAsync();
        if (path is not null) await _navigation.OpenReaderAsync(_book, _dto, path);
    }

    private async Task DownloadAsync()
    {
        var path = await EnsureDownloadedAsync();
        if (path is not null) await _navigation.AlertAsync("دریافت کامل شد", $"PDF برای مطالعه آفلاین در پوشه Downloads برنامه ذخیره شد.\n{path}");
    }

    private async Task<string?> EnsureDownloadedAsync()
    {
        if (IsDownloading) return null;
        IsDownloading = true; IsDownloadPanelVisible = true; DownloadStatus = "در حال دریافت PDF…";
        ReadCommand.NotifyCanExecuteChanged(); DownloadCommand.NotifyCanExecuteChanged();
        var progress = new Progress<double>(value =>
        {
            DownloadProgress = Math.Clamp(value, 0, 1);
            DownloadPercentText = $"{DownloadProgress * 100:0}%";
        });
        try
        {
            var path = await _assets.EnsurePdfAsync(_book, _dto, progress);
            DownloadStatus = "PDF آماده مطالعه آفلاین است"; DownloadPercentText = "۱۰۰٪"; DownloadButtonText = "دریافت شده";
            return path;
        }
        catch (Exception ex)
        {
            IsDownloadPanelVisible = false;
            await _navigation.AlertAsync("خطا در دریافت", ex.Message);
            return null;
        }
        finally
        {
            IsDownloading = false; ReadCommand.NotifyCanExecuteChanged(); DownloadCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task ToggleFavoriteAsync()
    {
        await _account.InitializeAsync();
        if (!_account.IsLoggedIn)
        {
            if (await _navigation.ConfirmAsync("حساب کاربری", "برای همگام‌سازی علاقه‌مندی‌ها وارد حساب شوید.", "ورود / ثبت‌نام", "فعلاً نه"))
                await _navigation.OpenAccountAsync(false);
            return;
        }
        try { IsFavorite = await _account.SetFavoriteAsync(_book.Id, !IsFavorite); }
        catch (Exception ex) { await _navigation.AlertAsync("علاقه‌مندی", ex.Message); }
    }

    private string BookUrl => string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url ?? string.Empty : _book.Url;
}
