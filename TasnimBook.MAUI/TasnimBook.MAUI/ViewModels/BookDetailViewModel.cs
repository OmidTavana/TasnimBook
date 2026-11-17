using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;
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
    private ImageSource _coverImage = ImageSource.FromFile("book_cover_placeholder.png");
    private bool _isFavorite, _isDownloading, _isDownloadPanelVisible;
    private string _progressText = "هنوز شروع نشده", _readButtonText = "شروع مطالعه در برنامه";
    private string _downloadStatus = "در حال دریافت PDF…", _downloadPercentText = "۰٪", _downloadButtonText = "دریافت";
    private double _downloadProgress;
    private int _likeCount, _ratingCount;
    private double _averageRating;
    private byte? _currentRating;
    private bool _isLiked;

    public bool IsAudioBook => _dto.IsAudioBook == true || _book.IsAudioBook == true;
    public bool IsPdfBook => !IsAudioBook;
    public AudioPlayerViewModel? AudioPlayer { get; }
    public string Title => _dto.Title;
    public string Author => BookUi.Author(_book);
    public string Pages => IsAudioBook ? "صوتی" : _dto.PagesCount ?? "—";
    public string PagesLabel => IsAudioBook ? "نوع محتوا" : "تعداد صفحات";
    public string Size => _dto.FileSize ?? "—";
    public string Score => RatingCount > 0 ? $"{AverageRating:0.0}/۵ ({RatingCount})" : "بدون امتیاز";
    public int LikeCount { get => _likeCount; private set => SetProperty(ref _likeCount, value); }
    public int RatingCount { get => _ratingCount; private set { if (SetProperty(ref _ratingCount, value)) OnPropertyChanged(nameof(Score)); } }
    public double AverageRating { get => _averageRating; private set { if (SetProperty(ref _averageRating, value)) OnPropertyChanged(nameof(Score)); } }
    public byte? CurrentRating { get => _currentRating; private set { if (SetProperty(ref _currentRating, value)) OnPropertyChanged(nameof(RatingText)); } }
    public string RatingText => CurrentRating.HasValue ? $"امتیاز شما: {CurrentRating} از ۵" : "امتیاز شما ثبت نشده";
    public bool IsLiked { get => _isLiked; private set { if (SetProperty(ref _isLiked, value)) OnPropertyChanged(nameof(LikeText)); } }
    public string LikeText => IsLiked ? $"پسندیده‌اید ({LikeCount})" : $"پسند ({LikeCount})";
    public string Description => string.IsNullOrWhiteSpace(_book.Description) ? _dto.Description ?? "توضیحی برای این کتاب ثبت نشده است." : _book.Description;
    public ImageSource CoverImage { get => _coverImage; private set => SetProperty(ref _coverImage, value); }
    public string ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }
    public string ReadButtonText { get => _readButtonText; private set => SetProperty(ref _readButtonText, value); }
    public bool IsFavorite { get => _isFavorite; private set { if (SetProperty(ref _isFavorite, value)) { OnPropertyChanged(nameof(FavoriteIcon)); OnPropertyChanged(nameof(HeaderFavoriteIcon)); OnPropertyChanged(nameof(FavoriteText)); } } }
    public string FavoriteIcon => IsFavorite ? "icon_heart_filled.png" : "icon_heart.png";
    public string HeaderFavoriteIcon => IsFavorite ? "icon_heart_header_filled.png" : "icon_heart_header.png";
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
    public AsyncCommand LikeCommand { get; }
    public AsyncCommand<string> RateCommand { get; }

    public BookDetailViewModel(Book book, BooksDTO dto, AccountService account, BookAssetService assets, AppNavigationService navigation)
    {
        _book = book; _dto = dto; _account = account; _assets = assets; _navigation = navigation;
        if (IsAudioBook) AudioPlayer = new AudioPlayerViewModel(book, dto, navigation);
        BackCommand = new AsyncCommand(_navigation.GoBackAsync);
        ReadCommand = new AsyncCommand(ReadAsync, () => !IsDownloading);
        DownloadCommand = new AsyncCommand(DownloadAsync, () => !IsDownloading);
        FavoriteCommand = new AsyncCommand(ToggleFavoriteAsync);
        ShareCommand = new AsyncCommand(() => _navigation.ShareAsync(Title, $"{Title}\n{Author}", BookUrl));
        LikeCommand = new AsyncCommand(ToggleLikeAsync);
        RateCommand = new AsyncCommand<string>(RateAsync);
    }

    public async Task OnAppearingAsync()
    {
        var imageUrl = string.IsNullOrWhiteSpace(_dto.ImageUrl) ? _book.ImageUrl : _dto.ImageUrl;
        var coverPath = await _assets.GetCoverPathAsync(_book.Id, imageUrl);
        CoverImage = string.IsNullOrWhiteSpace(coverPath) ? ImageSource.FromFile("book_cover_placeholder.png") : ImageSource.FromFile(coverPath);
        try
        {
            var progress = await _account.GetProgressAsync(_book.Id);
            ProgressText = IsAudioBook ? "کتاب صوتی" : progress > 0 ? $"{progress:0}% مطالعه شده" : "هنوز شروع نشده";
            ReadButtonText = progress > 0 ? "ادامه مطالعه" : "شروع مطالعه در برنامه";
            IsFavorite = await _account.IsFavoriteAsync(_book.Id);
            ApplyEngagement(await _account.GetEngagementAsync(_book.Id));
        }
        catch { }
        if (AudioPlayer is not null) await AudioPlayer.InitializeAsync();
    }

    public Task OnDisappearingAsync() => AudioPlayer?.PauseAndSaveAsync() ?? Task.CompletedTask;

    private async Task ReadAsync()
    {
        var path = await EnsureDownloadedAsync();
        if (path is not null)
        {
            await _account.RecordLibraryEventAsync(_book.Id, "Read");
            await _navigation.OpenReaderAsync(_book, _dto, path);
        }
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
            var wasDownloaded = File.Exists(_assets.GetPdfPath(_book.Id));
            var path = await _assets.EnsurePdfAsync(_book, _dto, progress);
            if (!wasDownloaded) await _account.RecordLibraryEventAsync(_book.Id, "Download");
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

    private async Task ToggleLikeAsync()
    {
        if (!await RequireAccountAsync()) return;
        try { ApplyEngagement(await _account.SetLikeAsync(_book.Id, !IsLiked)); }
        catch (Exception ex) { await _navigation.AlertAsync("پسند کتاب", ex.Message); }
    }

    private async Task RateAsync(string? value)
    {
        if (!await RequireAccountAsync()) return;
        try
        {
            byte? rating = byte.TryParse(value, out var parsed) ? parsed : null;
            ApplyEngagement(await _account.SetRatingAsync(_book.Id, rating));
        }
        catch (Exception ex) { await _navigation.AlertAsync("امتیاز کتاب", ex.Message); }
    }

    private async Task<bool> RequireAccountAsync()
    {
        await _account.InitializeAsync();
        if (_account.IsLoggedIn) return true;
        if (await _navigation.ConfirmAsync("حساب کاربری", "برای این قابلیت وارد حساب شوید.", "ورود / ثبت‌نام", "فعلاً نه"))
            await _navigation.OpenAccountAsync(false);
        return false;
    }

    private void ApplyEngagement(BookEngagementDto? state)
    {
        if (state is null) return;
        LikeCount = state.LikeCount; IsLiked = state.IsLikedByCurrentUser;
        RatingCount = state.RatingCount; AverageRating = state.AverageRating; CurrentRating = state.CurrentUserRating;
        OnPropertyChanged(nameof(LikeText));
    }

    private string BookUrl => string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url ?? string.Empty : _book.Url;
}
