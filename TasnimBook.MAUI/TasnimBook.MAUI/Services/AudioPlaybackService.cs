namespace TasnimBook.MAUI.Services;

public interface IAudioPlaybackService : IDisposable
{
    bool IsPlaying { get; }
    double PositionSeconds { get; }
    double DurationSeconds { get; }
    event EventHandler? PlaybackCompleted;
    Task LoadAsync(Uri source, double startPositionSeconds, CancellationToken cancellationToken = default);
    void Play();
    void Pause();
    void Seek(double positionSeconds);
}

public static class AudioPlaybackServiceFactory
{
    public static IAudioPlaybackService Create()
    {
#if ANDROID
        return new AndroidAudioPlaybackService();
#else
        return new UnsupportedAudioPlaybackService();
#endif
    }

    private sealed class UnsupportedAudioPlaybackService : IAudioPlaybackService
    {
        public bool IsPlaying => false;
        public double PositionSeconds => 0;
        public double DurationSeconds => 0;
        public event EventHandler? PlaybackCompleted;
        public Task LoadAsync(Uri source, double startPositionSeconds, CancellationToken cancellationToken = default) =>
            throw new PlatformNotSupportedException("پخش داخلی کتاب صوتی در این پلتفرم پشتیبانی نمی‌شود.");
        public void Play() { }
        public void Pause() { }
        public void Seek(double positionSeconds) { }
        public void Dispose() { }
    }
}

#if ANDROID
internal sealed class AndroidAudioPlaybackService : IAudioPlaybackService
{
    private Android.Media.MediaPlayer? _player;
    private bool _isPrepared;

    public bool IsPlaying => _isPrepared && _player?.IsPlaying == true;
    public double PositionSeconds => _isPrepared ? (_player?.CurrentPosition ?? 0) / 1000d : 0;
    public double DurationSeconds => _isPrepared ? (_player?.Duration ?? 0) / 1000d : 0;
    public event EventHandler? PlaybackCompleted;

    public async Task LoadAsync(Uri source, double startPositionSeconds, CancellationToken cancellationToken = default)
    {
        DisposePlayer();
        var player = new Android.Media.MediaPlayer();
        _player = player;

        using var attributes = new Android.Media.AudioAttributes.Builder()!
            .SetContentType(Android.Media.AudioContentType.Music)!
            .SetUsage(Android.Media.AudioUsageKind.Media)!
            .Build();
        player.SetAudioAttributes(attributes);
        player.SetDataSource(source.ToString());

        var prepared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? preparedHandler = null;
        preparedHandler = (_, _) => prepared.TrySetResult();
        player.Prepared += preparedHandler;
        player.Completion += OnCompletion;

        try
        {
            player.PrepareAsync();
            await prepared.Task.WaitAsync(TimeSpan.FromSeconds(45), cancellationToken);
            _isPrepared = true;
            if (startPositionSeconds > 0) Seek(startPositionSeconds);
        }
        finally
        {
            // Always detach before releasing MediaPlayer. Accessing an event on an
            // already released Java peer throws ObjectDisposedException.
            try { player.Prepared -= preparedHandler; } catch (ObjectDisposedException) { }
            if (!_isPrepared && ReferenceEquals(_player, player)) DisposePlayer();
        }
    }

    public void Play()
    {
        if (_isPrepared && _player is not null) _player.Start();
    }

    public void Pause()
    {
        if (IsPlaying) _player!.Pause();
    }

    public void Seek(double positionSeconds)
    {
        if (!_isPrepared || _player is null) return;
        var milliseconds = (int)Math.Clamp(positionSeconds * 1000d, 0, _player.Duration);
        _player.SeekTo(milliseconds);
    }

    private void OnCompletion(object? sender, EventArgs e) => PlaybackCompleted?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        DisposePlayer();
        GC.SuppressFinalize(this);
    }

    private void DisposePlayer()
    {
        _isPrepared = false;
        if (_player is null) return;
        try { _player.Completion -= OnCompletion; } catch (ObjectDisposedException) { }
        try { _player.Stop(); } catch { }
        try { _player.Reset(); } catch { }
        try { _player.Release(); } catch { }
        try { _player.Dispose(); } catch { }
        _player = null;
    }
}
#endif
