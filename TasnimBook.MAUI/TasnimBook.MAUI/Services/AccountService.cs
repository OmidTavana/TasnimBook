using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.MAUI.Services;

public sealed class AccountService
{
    private const string AccessTokenKey = "TasnimBook.Account.AccessToken";
    private const string RefreshTokenKey = "TasnimBook.Account.RefreshToken";
    private const string ProfileKey = "TasnimBook.Account.Profile";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;
    private string? _accessToken;
    private string? _refreshToken;

    public AccountProfileDto? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser is not null && !string.IsNullOrWhiteSpace(_accessToken);

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _gate.WaitAsync();
        try
        {
            if (_initialized) return;
            try
            {
                _accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
                _refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
                var profile = await SecureStorage.Default.GetAsync(ProfileKey);
                CurrentUser = string.IsNullOrWhiteSpace(profile)
                    ? null
                    : JsonConvert.DeserializeObject<AccountProfileDto>(profile);
            }
            catch
            {
                ClearLocalSession();
            }
            _initialized = true;
        }
        finally { _gate.Release(); }
    }

    public async Task<AccountProfileDto> LoginAsync(string email, string password)
    {
        var result = await PostPublicAsync<AccountAuthResult>("Account/Login", new LoginAccountRequest
        {
            Email = email.Trim(), Password = password, DeviceName = DeviceInfo.Current.Name
        });
        await SaveSessionAsync(result);
        return result.User;
    }

    public async Task<AccountProfileDto> RegisterAsync(string displayName, string email, string password)
    {
        var result = await PostPublicAsync<AccountAuthResult>("Account/Register", new RegisterAccountRequest
        {
            DisplayName = displayName.Trim(), Email = email.Trim(), Password = password,
            DeviceName = DeviceInfo.Current.Name
        });
        await SaveSessionAsync(result);
        return result.User;
    }

    public async Task LogoutAsync()
    {
        await InitializeAsync();
        if (!string.IsNullOrWhiteSpace(_accessToken) && !string.IsNullOrWhiteSpace(_refreshToken))
        {
            try
            {
                using var response = await SendAuthorizedAsync((client) => client.PostAsync(
                    "Account/Logout", JsonContent(new RefreshAccountRequest { RefreshToken = _refreshToken })));
            }
            catch { /* Local logout must still succeed if the server is unavailable. */ }
        }
        ClearLocalSession();
    }

    public async Task<IReadOnlyList<BooksDTO>> GetFavoritesAsync()
    {
        using var response = await SendAuthorizedAsync(client => client.GetAsync("Account/Favorites"));
        return await ReadAsync<List<BooksDTO>>(response) ?? [];
    }

    public async Task<bool> IsFavoriteAsync(int bookId)
    {
        if (!await HasSessionAsync()) return false;
        using var response = await SendAuthorizedAsync(client => client.GetAsync($"Account/FavoriteState/{bookId}"));
        return (await ReadAsync<FavoriteStateDto>(response))?.IsFavorite == true;
    }

    public async Task<bool> SetFavoriteAsync(int bookId, bool isFavorite)
    {
        using var response = await SendAuthorizedAsync(client => client.PutAsync(
            $"Account/Favorite/{bookId}", JsonContent(new FavoriteStateDto { BookId = bookId, IsFavorite = isFavorite })));
        return (await ReadAsync<FavoriteStateDto>(response))?.IsFavorite == true;
    }

    public async Task<decimal> GetProgressAsync(int bookId)
        => (await GetReadingProgressAsync(bookId)).PositionPercent;

    public async Task<ReadingProgressDto> GetReadingProgressAsync(int bookId)
    {
        if (!await HasSessionAsync())
            return new ReadingProgressDto
            {
                PositionPercent = (decimal)Preferences.Default.Get($"TasnimBook.Progress.{bookId}", 0d),
                CurrentPage = Preferences.Default.Get($"TasnimBook.Progress.Page.{bookId}", 0),
                TotalPages = Preferences.Default.Get($"TasnimBook.Progress.Total.{bookId}", 0)
            };

        using var response = await SendAuthorizedAsync(client => client.GetAsync($"Account/Progress/{bookId}"));
        if (response.StatusCode == HttpStatusCode.NotFound) return new ReadingProgressDto();
        return await ReadAsync<ReadingProgressDto>(response) ?? new ReadingProgressDto();
    }

    public async Task SaveProgressAsync(int bookId, decimal positionPercent, int? currentPage = null, int? totalPages = null)
    {
        positionPercent = Math.Clamp(positionPercent, 0, 100);
        if (!await HasSessionAsync())
        {
            Preferences.Default.Set($"TasnimBook.Progress.{bookId}", (double)positionPercent);
            if (currentPage.HasValue) Preferences.Default.Set($"TasnimBook.Progress.Page.{bookId}", currentPage.Value);
            if (totalPages.HasValue) Preferences.Default.Set($"TasnimBook.Progress.Total.{bookId}", totalPages.Value);
            return;
        }

        using var response = await SendAuthorizedAsync(client => client.PutAsync(
            $"Account/Progress/{bookId}", JsonContent(new ReadingProgressDto
            {
                PositionPercent = positionPercent,
                CurrentPage = currentPage,
                TotalPages = totalPages
            })));
        response.EnsureSuccessStatusCode();
    }

    private async Task<bool> HasSessionAsync()
    {
        await InitializeAsync();
        return IsLoggedIn;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
    {
        await InitializeAsync();
        if (!IsLoggedIn) throw new InvalidOperationException("برای استفاده از این قابلیت وارد حساب شوید.");

        using var firstClient = CreateClient(_accessToken);
        var response = await send(firstClient);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        response.Dispose();

        if (!await RefreshAsync()) throw new InvalidOperationException("نشست شما منقضی شده است؛ دوباره وارد شوید.");
        using var retryClient = CreateClient(_accessToken);
        return await send(retryClient);
    }

    private async Task<bool> RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_refreshToken)) return false;
        try
        {
            var result = await PostPublicAsync<AccountAuthResult>("Account/Refresh", new RefreshAccountRequest
            {
                RefreshToken = _refreshToken, DeviceName = DeviceInfo.Current.Name
            });
            await SaveSessionAsync(result);
            return true;
        }
        catch
        {
            ClearLocalSession();
            return false;
        }
    }

    private static StringContent JsonContent(object value) => new(
        JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

    private async Task<T> PostPublicAsync<T>(string path, object value)
    {
        using var client = CreateClient();
        using var response = await client.PostAsync(path, JsonContent(value));
        return await ReadAsync<T>(response) ?? throw new InvalidOperationException("پاسخ سرور معتبر نیست.");
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string? message = null;
            try { message = JsonConvert.DeserializeObject<Dictionary<string, object>>(text)?.Values.FirstOrDefault()?.ToString(); }
            catch { }
            throw new InvalidOperationException(message ?? "ارتباط با سرور ناموفق بود.");
        }
        return JsonConvert.DeserializeObject<T>(text);
    }

    private static HttpClient CreateClient(string? bearer = null)
    {
        var client = new HttpClient { BaseAddress = new Uri(ApiConfiguration.BaseAddress), Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Add("APIKey", ApiConfiguration.ApiKey);
        if (!string.IsNullOrWhiteSpace(bearer))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return client;
    }

    private async Task SaveSessionAsync(AccountAuthResult result)
    {
        _accessToken = result.AccessToken;
        _refreshToken = result.RefreshToken;
        CurrentUser = result.User;
        await SecureStorage.Default.SetAsync(AccessTokenKey, result.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);
        await SecureStorage.Default.SetAsync(ProfileKey, JsonConvert.SerializeObject(result.User));
    }

    private void ClearLocalSession()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ProfileKey);
        _accessToken = null;
        _refreshToken = null;
        CurrentUser = null;
    }
}
