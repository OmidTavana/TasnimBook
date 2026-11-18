using System.Net.Http.Headers;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Services;

public sealed class AdminApiClient
{
    private const string AccessTokenClaim = "access_token";
    private const string RefreshTokenClaim = "refresh_token";
    private const string AccessTokenExpiresClaim = "access_token_expires";
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<AccountAuthResult> LoginAsync(
        LoginAccountRequest loginRequest,
        CancellationToken cancellationToken)
    {
        const string path = "AdminAuth/Login";

        return SendAsync<AccountAuthResult>(
            HttpMethod.Post,
            path,
            loginRequest,
            cancellationToken,
            requiresAuthorization: false);
    }

    public async Task RegisterAsync(
        AdminRegisterRequest registrationRequest,
        CancellationToken cancellationToken)
    {
        const string path = "AdminAuth/Register";

        await SendAsync<object>(
            HttpMethod.Post,
            path,
            registrationRequest,
            cancellationToken,
            requiresAuthorization: false);
    }

    public Task<DashboardDto> DashboardAsync(CancellationToken cancellationToken)
    {
        const string path = "Dashboard/Overview";

        return GetAsync<DashboardDto>(path, cancellationToken);
    }

    public Task<IReadOnlyList<AdminUserDto>> PendingAsync(CancellationToken cancellationToken)
    {
        const string path = "AdminAuth/Pending";

        return GetAsync<IReadOnlyList<AdminUserDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<AdminUserDto>> AdministratorsAsync(CancellationToken cancellationToken)
    {
        const string path = "AdminAuth/Administrators";

        return GetAsync<IReadOnlyList<AdminUserDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<AppNotification>> NotificationsAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<AppNotification>>("AdminNotifications", cancellationToken);

    public async Task CreateNotificationAsync(AppNotification notification, CancellationToken cancellationToken) =>
        await SendAsync<object>(HttpMethod.Post, "AdminNotifications", notification, cancellationToken);

    public async Task DeleteNotificationAsync(int id, CancellationToken cancellationToken) =>
        await SendAsync<object>(HttpMethod.Delete, $"AdminNotifications/{id}", body: null, cancellationToken);

    public async Task ReviewAsync(
        int administratorId,
        AdminReviewRequest reviewRequest,
        CancellationToken cancellationToken)
    {
        var path = $"AdminAuth/Review/{administratorId}";

        await SendAsync<object>(
            HttpMethod.Put,
            path,
            reviewRequest,
            cancellationToken);
    }

    public Task<PagedResult<Book>> BooksAsync(
        string? search,
        int page,
        CancellationToken cancellationToken)
    {
        var searchTerm = EncodeSearchTerm(search);
        var path = $"AdminContent/Books?search={searchTerm}&page={page}";

        return GetAsync<PagedResult<Book>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<Author>> AuthorsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var searchTerm = EncodeSearchTerm(search);
        var path = $"AdminContent/Authors?search={searchTerm}";

        return GetAsync<IReadOnlyList<Author>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<BookCat>> CategoriesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var searchTerm = EncodeSearchTerm(search);
        var path = $"AdminContent/Categories?search={searchTerm}";

        return GetAsync<IReadOnlyList<BookCat>>(path, cancellationToken);
    }

    public async Task DeleteAsync(
        string entityName,
        int entityId,
        CancellationToken cancellationToken)
    {
        var path = $"AdminContent/{entityName}/{entityId}";

        await SendAsync<object>(HttpMethod.Delete, path, body: null, cancellationToken);
    }

    public Task<Book> BookAsync(int bookId, CancellationToken cancellationToken)
    {
        var path = $"AdminContent/GetBook/{bookId}";

        return GetAsync<Book>(path, cancellationToken);
    }

    public Task<Author> AuthorAsync(int authorId, CancellationToken cancellationToken)
    {
        var path = $"AdminContent/GetAuthor/{authorId}";

        return GetAsync<Author>(path, cancellationToken);
    }

    public Task<BookCat> CategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var path = $"AdminContent/GetCategory/{categoryId}";

        return GetAsync<BookCat>(path, cancellationToken);
    }

    public async Task SaveBookAsync(
        Book book,
        int[] categoryIds,
        CancellationToken cancellationToken)
    {
        var method = book.Id == 0 ? HttpMethod.Post : HttpMethod.Put;
        var path = book.Id == 0
            ? "AdminContent/Book"
            : $"AdminContent/Book/{book.Id}";

        var categoryQuery = string.Join(
            "&",
            categoryIds.Select(categoryId => $"categoryIds={categoryId}"));

        if (!string.IsNullOrEmpty(categoryQuery))
        {
            path = $"{path}?{categoryQuery}";
        }

        await SendAsync<object>(method, path, book, cancellationToken);
    }

    public async Task SaveAuthorAsync(Author author, CancellationToken cancellationToken)
    {
        var method = author.Id == 0 ? HttpMethod.Post : HttpMethod.Put;
        var path = author.Id == 0
            ? "AdminContent/Author"
            : $"AdminContent/Author/{author.Id}";

        await SendAsync<object>(method, path, author, cancellationToken);
    }

    public async Task SaveCategoryAsync(BookCat category, CancellationToken cancellationToken)
    {
        var method = category.Id == 0 ? HttpMethod.Post : HttpMethod.Put;
        var path = category.Id == 0
            ? "AdminContent/Category"
            : $"AdminContent/Category/{category.Id}";

        await SendAsync<object>(method, path, category, cancellationToken);
    }

    public async Task<AdminAssetUploadDto> UploadAssetAsync(
        IFormFile file,
        string kind,
        string bookTitle,
        CancellationToken cancellationToken)
    {
        var path = $"AdminAssets/Upload?kind={Uri.EscapeDataString(kind)}&bookTitle={Uri.EscapeDataString(bookTitle)}";

        using var fileStream = file.OpenReadStream();
        using var fileContent = new StreamContent(fileStream);
        using var formContent = new MultipartFormDataContent();

        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        formContent.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = formContent
        };

        AddApiKey(request);
        await AddAccessTokenAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return ReadResponse<AdminAssetUploadDto>(response, responseContent);
    }

    public string? ResolveAssetUrl(string? assetUrl)
    {
        if (string.IsNullOrWhiteSpace(assetUrl))
        {
            return null;
        }

        if (Uri.TryCreate(assetUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return _httpClient.BaseAddress is null
            ? assetUrl
            : new Uri(_httpClient.BaseAddress, assetUrl).ToString();
    }

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        return SendAsync<T>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        bool requiresAuthorization = true)
    {
        using var request = new HttpRequestMessage(method, path);

        AddApiKey(request);

        if (requiresAuthorization)
        {
            await AddAccessTokenAsync(request, cancellationToken);
        }

        if (body is not null)
        {
            request.Content = CreateJsonContent(body);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return ReadResponse<T>(response, responseContent);
    }

    private void AddApiKey(HttpRequestMessage request)
    {
        var apiKey = _configuration["Api:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Api:ApiKey is not configured.");
        }

        request.Headers.Add("APIKey", apiKey);
    }

    private async Task AddAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var accessToken = context?.User.FindFirst(AccessTokenClaim)?.Value;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new UnauthorizedAccessException("نشست مدیریت معتبر نیست؛ لطفاً دوباره وارد شوید.");
        }

        var expiresValue = context?.User.FindFirst(AccessTokenExpiresClaim)?.Value;
        if (DateTime.TryParse(expiresValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt) &&
            expiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            accessToken = await RefreshAccessTokenAsync(context!, cancellationToken);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<string> RefreshAccessTokenAsync(HttpContext context, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            var currentExpires = context.User.FindFirst(AccessTokenExpiresClaim)?.Value;
            if (DateTime.TryParse(currentExpires, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var currentExpiry) &&
                currentExpiry > DateTime.UtcNow.AddMinutes(1))
                return context.User.FindFirst(AccessTokenClaim)!.Value;

            var refreshToken = context.User.FindFirst(RefreshTokenClaim)?.Value;
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("نشست مدیریت منقضی شده است؛ لطفاً خارج شوید و دوباره وارد شوید.");

            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "Account/Refresh")
            {
                Content = CreateJsonContent(new RefreshAccountRequest { RefreshToken = refreshToken, DeviceName = "TasnimBook.Admin" })
            };
            AddApiKey(refreshRequest);
            using var response = await _httpClient.SendAsync(refreshRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = ReadResponse<AccountAuthResult>(response, content);

            if (result.User.Role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin))
                throw new UnauthorizedAccessException("دسترسی مدیریت این حساب غیرفعال شده است.");

            var principal = CreatePrincipal(result);
            context.User = principal;
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return result.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static ClaimsPrincipal CreatePrincipal(AccountAuthResult result)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.User.DisplayName),
            new Claim(ClaimTypes.Email, result.User.Email),
            new Claim(ClaimTypes.Role, result.User.Role),
            new Claim(AccessTokenClaim, result.AccessToken),
            new Claim(RefreshTokenClaim, result.RefreshToken),
            new Claim(AccessTokenExpiresClaim, result.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture))
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static StringContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);

        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static T ReadResponse<T>(HttpResponseMessage response, string responseContent)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = ExtractErrorMessage(response, responseContent);
            throw new AdminApiException((int)response.StatusCode, message);
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseContent))
        {
            return default!;
        }

        var result = JsonSerializer.Deserialize<T>(responseContent, JsonOptions);

        return result ?? throw new InvalidOperationException("The API returned an invalid response.");
    }

    private static string EncodeSearchTerm(string? search)
    {
        return Uri.EscapeDataString(search ?? string.Empty);
    }

    private static string ExtractErrorMessage(HttpResponseMessage response, string responseContent)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);

            if (document.RootElement.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                var validationMessages = errorsElement
                    .EnumerateObject()
                    .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray().Select(item => item.GetString())
                        : [property.Value.GetString()])
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray();

                if (validationMessages.Length > 0)
                    return string.Join(" | ", validationMessages);
            }

            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? "درخواست انجام نشد.";
            }
            if (document.RootElement.TryGetProperty("detail", out var detailElement))
                return detailElement.GetString() ?? "درخواست انجام نشد.";
            if (document.RootElement.TryGetProperty("title", out var titleElement))
                return titleElement.GetString() ?? "درخواست انجام نشد.";
        }
        catch (JsonException)
        {
            // The API did not return a JSON error body.
        }

        return (int)response.StatusCode switch
        {
            401 => "نشست مدیریت منقضی شده است؛ لطفاً دوباره وارد شوید.",
            403 => "شما اجازه انجام این عملیات را ندارید.",
            413 => "حجم فایل از سقف مجاز سرور بیشتر است.",
            415 => "فرمت فایل توسط سرور پشتیبانی نمی‌شود.",
            >= 500 => $"ذخیره در سرور انجام نشد (خطای {(int)response.StatusCode}).",
            _ => $"درخواست انجام نشد (خطای {(int)response.StatusCode})."
        };
    }
}

public sealed class AdminApiException : Exception
{
    public AdminApiException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
