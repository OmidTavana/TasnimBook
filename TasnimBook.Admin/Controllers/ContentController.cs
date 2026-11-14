using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Admin.Services;
using TasnimBook.Data;
using TasnimBook.Admin.Models;

namespace TasnimBook.Admin.Controllers;

[Authorize(Roles = UserRoles.Administrators)]
public sealed class ContentController(AdminApiClient api) : Controller
{
    public async Task<IActionResult> Books(string? search, int page = 1, CancellationToken token = default) { ViewBag.Search = search; return View(await api.BooksAsync(search, page, token)); }
    public async Task<IActionResult> Authors(string? search, CancellationToken token) { ViewBag.Search = search; return View(await api.AuthorsAsync(search, token)); }
    public async Task<IActionResult> Categories(string? search, CancellationToken token) { ViewBag.Search = search; return View(await api.CategoriesAsync(search, token)); }
    [HttpGet] public IActionResult CreateBook() => View("EditBook", new BookEditorModel());
    [HttpGet] public async Task<IActionResult> EditBook(int id, CancellationToken token) { var b = await api.BookAsync(id, token); return View(new BookEditorModel { Id = b.Id, Title = b.Title, Description = b.Description, Url = b.Url, ImageUrl = b.ImageUrl, FileSize = b.FileSize, PagesCount = b.PagesCount, AuthorId = b.AuthorId, IsAudioBook = b.IsAudioBook == true, IsSuggested = b.IsSuggested == true, CategoryIds = string.Join(',', b.BookCategories.Select(link => link.BookCategoryId)) }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditBook(BookEditorModel model, CancellationToken token) { if (!ModelState.IsValid) return View(model); await api.SaveBookAsync(model.ToEntity(), model.ParsedCategoryIds(), token); TempData["Success"] = "کتاب ذخیره شد."; return RedirectToAction(nameof(Books)); }
    [HttpGet] public async Task<IActionResult> EditAuthor(int? id, CancellationToken token) => View(id.HasValue ? await api.AuthorAsync(id.Value, token) : new Author());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditAuthor(Author model, CancellationToken token) { await api.SaveAuthorAsync(model, token); TempData["Success"] = "نویسنده ذخیره شد."; return RedirectToAction(nameof(Authors)); }
    [HttpGet] public async Task<IActionResult> EditCategory(int? id, CancellationToken token) => View(id.HasValue ? await api.CategoryAsync(id.Value, token) : new BookCat());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EditCategory(BookCat model, CancellationToken token) { await api.SaveCategoryAsync(model, token); TempData["Success"] = "دسته ذخیره شد."; return RedirectToAction(nameof(Categories)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(string entity, int id, CancellationToken token) { try { await api.DeleteAsync(entity, id, token); TempData["Success"] = "حذف انجام شد."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(entity + "s"); }
}
