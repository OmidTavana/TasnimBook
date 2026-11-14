using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Services;

public sealed class AdminApiClient(HttpClient http, IConfiguration configuration, IHttpContextAccessor context)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public Task<AccountAuthResult> LoginAsync(LoginAccountRequest request, CancellationToken token) =>
        SendAsync<AccountAuthResult>(HttpMethod.Post, "AdminAuth/Login", request, token, false);
    public Task RegisterAsync(AdminRegisterRequest request, CancellationToken token) =>
        SendAsync<object>(HttpMethod.Post, "AdminAuth/Register", request, token, false);
    public Task<DashboardDto> DashboardAsync(CancellationToken token) => SendAsync<DashboardDto>(HttpMethod.Get, "Dashboard/Overview", null, token);
    public Task<IReadOnlyList<AdminUserDto>> PendingAsync(CancellationToken token) => SendAsync<IReadOnlyList<AdminUserDto>>(HttpMethod.Get, "AdminAuth/Pending", null, token);
    public Task<IReadOnlyList<AdminUserDto>> AdministratorsAsync(CancellationToken token) => SendAsync<IReadOnlyList<AdminUserDto>>(HttpMethod.Get, "AdminAuth/Administrators", null, token);
    public Task ReviewAsync(int id, AdminReviewRequest request, CancellationToken token) => SendAsync<object>(HttpMethod.Put, $"AdminAuth/Review/{id}", request, token);
    public Task<PagedResult<TasnimBook.Data.Book>> BooksAsync(string? search, int page, CancellationToken token) => SendAsync<PagedResult<TasnimBook.Data.Book>>(HttpMethod.Get, $"AdminContent/Books?search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}", null, token);
    public Task<IReadOnlyList<TasnimBook.Data.Author>> AuthorsAsync(string? search, CancellationToken token) => SendAsync<IReadOnlyList<TasnimBook.Data.Author>>(HttpMethod.Get, $"AdminContent/Authors?search={Uri.EscapeDataString(search ?? string.Empty)}", null, token);
    public Task<IReadOnlyList<TasnimBook.Data.BookCat>> CategoriesAsync(string? search, CancellationToken token) => SendAsync<IReadOnlyList<TasnimBook.Data.BookCat>>(HttpMethod.Get, $"AdminContent/Categories?search={Uri.EscapeDataString(search ?? string.Empty)}", null, token);
    public Task DeleteAsync(string entity, int id, CancellationToken token) => SendAsync<object>(HttpMethod.Delete, $"AdminContent/{entity}/{id}", null, token);
    public Task<TasnimBook.Data.Book> BookAsync(int id, CancellationToken token) => SendAsync<TasnimBook.Data.Book>(HttpMethod.Get, $"AdminContent/GetBook/{id}", null, token);
    public Task<TasnimBook.Data.Author> AuthorAsync(int id, CancellationToken token) => SendAsync<TasnimBook.Data.Author>(HttpMethod.Get, $"AdminContent/GetAuthor/{id}", null, token);
    public Task<TasnimBook.Data.BookCat> CategoryAsync(int id, CancellationToken token) => SendAsync<TasnimBook.Data.BookCat>(HttpMethod.Get, $"AdminContent/GetCategory/{id}", null, token);
    public Task SaveBookAsync(TasnimBook.Data.Book book, int[] categoryIds, CancellationToken token) => SendAsync<object>(book.Id == 0 ? HttpMethod.Post : HttpMethod.Put, $"AdminContent/Book{(book.Id == 0 ? string.Empty : "/" + book.Id)}?{string.Join("&", categoryIds.Select(id => "categoryIds=" + id))}", book, token);
    public Task SaveAuthorAsync(TasnimBook.Data.Author author, CancellationToken token) => SendAsync<object>(author.Id == 0 ? HttpMethod.Post : HttpMethod.Put, $"AdminContent/Author{(author.Id == 0 ? string.Empty : "/" + author.Id)}", author, token);
    public Task SaveCategoryAsync(TasnimBook.Data.BookCat category, CancellationToken token) => SendAsync<object>(category.Id == 0 ? HttpMethod.Post : HttpMethod.Put, $"AdminContent/Category{(category.Id == 0 ? string.Empty : "/" + category.Id)}", category, token);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken token, bool authorized = true)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("APIKey", configuration["Api:ApiKey"] ?? throw new InvalidOperationException("Api:ApiKey is not configured."));
        if (authorized)
        {
            var accessToken = context.HttpContext?.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrWhiteSpace(accessToken)) throw new UnauthorizedAccessException();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, token);
        var content = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode) throw new AdminApiException((int)response.StatusCode, ExtractMessage(content));
        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(content)) return default!;
        return JsonSerializer.Deserialize<T>(content, Json) ?? throw new InvalidOperationException("Invalid API response.");
    }

    private static string ExtractMessage(string content)
    {
        try { return JsonDocument.Parse(content).RootElement.GetProperty("message").GetString() ?? "درخواست انجام نشد."; }
        catch { return "درخواست انجام نشد."; }
    }
}

public sealed class AdminApiException(int statusCode, string message) : Exception(message) { public int StatusCode { get; } = statusCode; }
