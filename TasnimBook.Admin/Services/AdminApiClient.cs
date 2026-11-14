using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TasnimBook.Data;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Services;

public sealed class AdminApiClient
{
    private const string AccessTokenClaim = "access_token";

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
            AddAccessToken(request);
        }

        if (body is not null)
        {
            request.Content = CreateJsonContent(body);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = ExtractErrorMessage(responseContent);
            throw new AdminApiException((int)response.StatusCode, message);
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseContent))
        {
            return default!;
        }

        var result = JsonSerializer.Deserialize<T>(responseContent, JsonOptions);

        return result ?? throw new InvalidOperationException("The API returned an invalid response.");
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

    private void AddAccessToken(HttpRequestMessage request)
    {
        var accessToken = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(AccessTokenClaim)?
            .Value;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new UnauthorizedAccessException("The administrator is not signed in.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static StringContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);

        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static string EncodeSearchTerm(string? search)
    {
        return Uri.EscapeDataString(search ?? string.Empty);
    }

    private static string ExtractErrorMessage(string responseContent)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);

            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? "درخواست انجام نشد.";
            }
        }
        catch (JsonException)
        {
            // The API did not return a JSON error body.
        }

        return "درخواست انجام نشد.";
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
