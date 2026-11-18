using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Models;
using TasnimBook.Admin.Services;
using TasnimBook.Data;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class ContentController : Controller
{
    private const long MaximumBookUploadSize = 310L * 1024 * 1024;
    private readonly AdminApiClient _apiClient;

    public ContentController(AdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Books(
        string? search,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ViewBag.Search = search;
        ViewBag.AssetBaseUrl = _apiClient.ResolveAssetUrl("/");

        var books = await _apiClient.BooksAsync(search, page, cancellationToken);

        return View(books);
    }

    public async Task<IActionResult> Authors(
        string? search,
        CancellationToken cancellationToken)
    {
        ViewBag.Search = search;

        var authors = await _apiClient.AuthorsAsync(search, cancellationToken);

        return View(authors);
    }

    public async Task<IActionResult> Categories(
        string? search,
        CancellationToken cancellationToken)
    {
        ViewBag.Search = search;

        var categories = await _apiClient.CategoriesAsync(search, cancellationToken);

        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> CreateBook(CancellationToken cancellationToken)
    {
        var model = new BookEditorModel();
        await PrepareBookEditorAsync(model, cancellationToken);

        return View("EditBook", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditBook(
        int id,
        CancellationToken cancellationToken)
    {
        var book = await _apiClient.BookAsync(id, cancellationToken);
        var model = BookEditorModel.FromEntity(book);

        await PrepareBookEditorAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumBookUploadSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumBookUploadSize)]
    public async Task<IActionResult> EditBook(
        BookEditorModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PrepareBookEditorAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            if (model.CoverImage is not null)
            {
                var cover = await _apiClient.UploadAssetAsync(
                    model.CoverImage,
                    "cover",
                    model.Title,
                    cancellationToken);

                model.ImageUrl = cover.Url;
                model.CoverPreviewUrl = cover.Url;
            }

            if (model.BookFile is not null)
            {
                var document = await _apiClient.UploadAssetAsync(
                    model.BookFile,
                    model.IsAudioBook ? "audio" : "document",
                    model.Title,
                    cancellationToken);

                model.Url = document.Url;
                model.FileSize = FormatFileSize(document.Size);
            }

            var book = model.ToEntity();

            await _apiClient.SaveBookAsync(book, model.CategoryIds, cancellationToken);
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PrepareBookEditorAsync(model, cancellationToken);

            return View(model);
        }

        TempData["Success"] = "کتاب ذخیره شد.";
        return RedirectToAction(nameof(Books));
    }

    [HttpGet]
    public async Task<IActionResult> EditAuthor(
        int? id,
        CancellationToken cancellationToken)
    {
        var author = id.HasValue
            ? await _apiClient.AuthorAsync(id.Value, cancellationToken)
            : new Author();

        return View(author);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAuthor(
        Author author,
        CancellationToken cancellationToken)
    {
        await _apiClient.SaveAuthorAsync(author, cancellationToken);

        TempData["Success"] = "نویسنده ذخیره شد.";
        return RedirectToAction(nameof(Authors));
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(
        int? id,
        CancellationToken cancellationToken)
    {
        var category = id.HasValue
            ? await _apiClient.CategoryAsync(id.Value, cancellationToken)
            : new BookCat();

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(
        BookCat category,
        CancellationToken cancellationToken)
    {
        await _apiClient.SaveCategoryAsync(category, cancellationToken);

        TempData["Success"] = "دسته ذخیره شد.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string entity,
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync(entity, id, cancellationToken);
            TempData["Success"] = "حذف انجام شد.";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
        }

        var actionName = GetListActionName(entity);
        return RedirectToAction(actionName);
    }

    private static string GetListActionName(string entity)
    {
        return entity switch
        {
            "Book" => nameof(Books),
            "Author" => nameof(Authors),
            "Category" => nameof(Categories),
            _ => nameof(Books)
        };
    }

    private async Task PrepareBookEditorAsync(
        BookEditorModel model,
        CancellationToken cancellationToken)
    {
        var authorsTask = _apiClient.AuthorsAsync(search: null, cancellationToken);
        var categoriesTask = _apiClient.CategoriesAsync(search: null, cancellationToken);

        await Task.WhenAll(authorsTask, categoriesTask);

        model.AvailableAuthors = await authorsTask;
        model.AvailableCategories = await categoriesTask;
        model.CoverPreviewUrl = _apiClient.ResolveAssetUrl(model.ImageUrl);
    }

    private static string FormatFileSize(long bytes)
    {
        const double bytesInMegabyte = 1024d * 1024d;

        return $"{bytes / bytesInMegabyte:0.##} MB";
    }
}
