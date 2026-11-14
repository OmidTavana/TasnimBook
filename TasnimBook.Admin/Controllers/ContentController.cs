using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Models;
using TasnimBook.Admin.Services;
using TasnimBook.Data;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class ContentController : Controller
{
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
    public IActionResult CreateBook()
    {
        return View("EditBook", new BookEditorModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditBook(
        int id,
        CancellationToken cancellationToken)
    {
        var book = await _apiClient.BookAsync(id, cancellationToken);
        var model = BookEditorModel.FromEntity(book);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBook(
        BookEditorModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var book = model.ToEntity();
        var categoryIds = model.ParseCategoryIds();

        await _apiClient.SaveBookAsync(book, categoryIds, cancellationToken);

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
}
