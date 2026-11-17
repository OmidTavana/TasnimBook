using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.ViewModels;

public sealed class AudioPlayerViewModel : ObservableObject
{
    private readonly Book _book;
    private readonly BooksDTO _dto;
    private readonly AppNavigationService _navigation;
    private readonly IAudioPlaybackService _player = AudioPlaybackServiceFactory.Create();
    private readonly string _positionKey;
    private CancellationTokenSource? _progressCancellation;
    private bool _isInitialized;
    private bool _isBusy;
    private bool _isPlaying;
    private bool _isSeeking;
    private double _positionSeconds;
    private double _durationSeconds = 1;
    private string _statusMessage = "در حال آماده‌سازی فایل صوتی…";

    public string Title => _dto.Title;
    public string Author => BookUi.Author(_book);
    public string Cover => ApiConfiguration.ResolveResourceUrl(_dto.ImageUrl) ?? "book_cover_placeholder.png";
    public string PlayIcon => _isPlaying ? "icon_pause.png" : "icon_play.png";
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public bool IsReady => _isInitialized && !IsBusy;
    public double PositionSeconds { get => _positionSeconds; set => SetProperty(ref _positionSeconds, value); }
    public double DurationSeconds { get => _durationSeconds; private set => SetProperty(ref _durationSeconds, Math.Max(1, value)); }
    public string PositionText => FormatTime(PositionSeconds);
    public string DurationText => FormatTime(DurationSeconds);
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    public AsyncCommand PlayCommand { get; }
    public AsyncCommand SeekBackwardCommand { get; }
    public AsyncCommand SeekForwardCommand { get; }

    public AudioPlayerViewModel(Book book, BooksDTO dto, AppNavigationService navigation)
    {
        _book = book;
        _dto = dto;
        _navigation = navigation;
        _positionKey = $"TasnimBook.AudioPosition.{dto.Id}";
        PlayCommand = new AsyncCommand(TogglePlaybackAsync);
        SeekBackwardCommand = new AsyncCommand(() => SeekRelativeAsync(-15));
        SeekForwardCommand = new AsyncCommand(() => SeekRelativeAsync(15));
        _player.PlaybackCompleted += OnPlaybackCompleted;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            if (PositionSeconds > 0 && !_player.IsPlaying)
            {
                _player.Play();
                StatusMessage = "ادامه پخش از موقعیت ذخیره‌شده";
                UpdatePlaybackState();
            }
            return;
        }
        if (IsBusy) return;
        var url = string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url : _book.Url;
        if (!TryResolveUri(url, out var uri))
        {
            StatusMessage = "فایل صوتی برای این عنوان موجود نیست.";
            return;
        }

        IsBusy = true;
        OnPropertyChanged(nameof(IsReady));
        try
        {
            var savedPosition = Preferences.Default.Get(_positionKey, 0d);
            await _player.LoadAsync(uri, savedPosition);
            _isInitialized = true;
            DurationSeconds = _player.DurationSeconds;
            PositionSeconds = _player.PositionSeconds;
            NotifyProgressChanged();
            if (savedPosition > 0)
            {
                _player.Play();
                StatusMessage = "ادامه پخش از موقعیت ذخیره‌شده";
                UpdatePlaybackState();
            }
            else
            {
                StatusMessage = "آماده پخش";
            }
            StartProgressLoop();
        }
        catch (Exception exception)
        {
            StatusMessage = "آماده‌سازی فایل صوتی ناموفق بود.";
            await _navigation.AlertAsync("پخش کتاب صوتی", exception.Message);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsReady));
        }
    }

    private async Task TogglePlaybackAsync()
    {
        if (!_isInitialized) await InitializeAsync();
        if (!_isInitialized) return;

        if (_player.IsPlaying)
        {
            _player.Pause();
            SavePosition();
            StatusMessage = "پخش متوقف شده است";
        }
        else
        {
            _player.Play();
            StatusMessage = "در حال پخش";
        }
        UpdatePlaybackState();
    }

    public void BeginSeek() => _isSeeking = true;

    public Task CommitSeekAsync(double positionSeconds)
    {
        if (_isInitialized)
        {
            _player.Seek(positionSeconds);
            PositionSeconds = positionSeconds;
            NotifyProgressChanged();
            SavePosition();
        }
        _isSeeking = false;
        return Task.CompletedTask;
    }

    public Task PauseAndSaveAsync()
    {
        if (_isInitialized)
        {
            _player.Pause();
            SavePosition();
            UpdatePlaybackState();
            StatusMessage = "موقعیت پخش ذخیره شد";
        }
        return Task.CompletedTask;
    }

    private Task SeekRelativeAsync(double seconds) =>
        CommitSeekAsync(Math.Clamp(PositionSeconds + seconds, 0, DurationSeconds));

    private void StartProgressLoop()
    {
        _progressCancellation?.Cancel();
        _progressCancellation = new CancellationTokenSource();
        var token = _progressCancellation.Token;
        _ = Task.Run(async () =>
        {
            var saveCounter = 0;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, token);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (!_isSeeking)
                        {
                            PositionSeconds = _player.PositionSeconds;
                            DurationSeconds = _player.DurationSeconds;
                            NotifyProgressChanged();
                        }
                        UpdatePlaybackState();
                        if (_player.IsPlaying && ++saveCounter >= 4)
                        {
                            SavePosition();
                            saveCounter = 0;
                        }
                    });
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void UpdatePlaybackState()
    {
        var isPlaying = _player.IsPlaying;
        if (_isPlaying == isPlaying) return;
        _isPlaying = isPlaying;
        OnPropertyChanged(nameof(PlayIcon));
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Preferences.Default.Remove(_positionKey);
            PositionSeconds = 0;
            _player.Seek(0);
            StatusMessage = "پخش به پایان رسید";
            NotifyProgressChanged();
            UpdatePlaybackState();
        });
    }

    private void SavePosition()
    {
        if (_isInitialized) Preferences.Default.Set(_positionKey, _player.PositionSeconds);
    }

    private void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(PositionText));
        OnPropertyChanged(nameof(DurationText));
    }

    private static string FormatTime(double seconds)
    {
        var value = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
    }

    private static bool TryResolveUri(string? value, out Uri uri)
    {
        return ApiConfiguration.TryResolveResourceUri(value, out uri);
    }
}
