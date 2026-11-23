using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;
using TasnimBook.Data.SqlRepository;

namespace TasnimBook.MAUI.Services;

public sealed class ApiSessionService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IRepository? _repository;

    public async Task<IRepository> GetRepositoryAsync()
    {
        if (_repository is not null) return _repository;
        await _gate.WaitAsync();
        try
        {
            if (_repository is not null) return _repository;
            var baseAddress = ApiConfiguration.BaseAddress;
            var apiKey = ApiConfiguration.ApiKey;
            var deviceId = Preferences.Default.Get("TasnimBook.DeviceId", Guid.NewGuid().ToString("N"));
            Preferences.Default.Set("TasnimBook.DeviceId", deviceId);
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("APIKey", apiKey);
            var response = await client.PostAsync("JwtUser/Register/" + Uri.EscapeDataString(deviceId), null);
            var responseBody = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(responseBody);
            response.EnsureSuccessStatusCode();
            var auth = JsonConvert.DeserializeObject<AuthResult>(responseBody);
            if (string.IsNullOrWhiteSpace(auth?.Token)) throw new InvalidOperationException("پاسخ توکن API معتبر نیست.");
            _repository = new RestRepository(baseAddress, apiKey, auth.Token);
            return _repository;
        }
        finally { _gate.Release(); }
    }

    public void Reset()
    {
        var previous = Interlocked.Exchange(ref _repository, null);
        (previous as IDisposable)?.Dispose();
    }

    public void Reset(IRepository failedRepository)
    {
        var previous = Interlocked.CompareExchange(ref _repository, null, failedRepository);
        if (ReferenceEquals(previous, failedRepository))
            (previous as IDisposable)?.Dispose();
    }
}
