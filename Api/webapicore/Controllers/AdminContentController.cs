using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;

namespace webapicore.Controllers;

[ApiController, ApiKeyAuth, Authorize(Roles = UserRoles.Administrators), Route("api/[controller]/[action]")]
public sealed class AdminContentController(DbContextAnti db, ILogger<AdminContentController> logger) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBook(int id, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().Include(item => item.Author).Include(item => item.BookCategories)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpGet]
    public async Task<IActionResult> Books(string? search, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Books.AsNoTracking().Include(book => book.Author).Include(book => book.BookCategories).ThenInclude(link => link.BookCategories).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(book => book.Title.Contains(search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(book => book.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new PagedResult<Book> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpPost]
    public async Task<IActionResult> Book(
        [FromBody] Book book,
        [FromQuery] int[] categoryIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(book.Title)) return BadRequest(new { message = "عنوان الزامی است." });
        book.Id = 0;
        book.BookCategories = categoryIds.Distinct().Select(id => new ManyToManyBookAndBookCatcs { BookCategoryId = id }).ToList();
        try
        {
            db.Books.Add(book);
            await db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(Books), new { search = book.Title }, new { book.Id });
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Creating book {BookTitle} failed", book.Title);
            return Conflict(new { message = "ذخیره کتاب در پایگاه داده انجام نشد؛ نویسنده و دسته‌بندی‌های انتخاب‌شده را بررسی کنید." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Book(
        int id,
        [FromBody] Book input,
        [FromQuery] int[] categoryIds,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.Include(item => item.BookCategories).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null) return NotFound();
        book.Title = input.Title; book.Description = input.Description; book.Url = input.Url; book.ImageUrl = input.ImageUrl;
        book.Date = input.Date; book.FileSize = input.FileSize; book.PagesCount = input.PagesCount; book.AuthorId = input.AuthorId;
        book.IsAudioBook = input.IsAudioBook; book.IsSuggested = input.IsSuggested;
        db.BookCategoryRelations.RemoveRange(book.BookCategories);
        book.BookCategories = categoryIds.Distinct().Select(categoryId => new ManyToManyBookAndBookCatcs { BookId = id, BookCategoryId = categoryId }).ToList();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Updating book {BookId} failed", id);
            return Conflict(new { message = "ویرایش کتاب در پایگاه داده انجام نشد؛ نویسنده و دسته‌بندی‌های انتخاب‌شده را بررسی کنید." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Book(int id, CancellationToken cancellationToken)
    {
        var book = await db.Books.FindAsync([id], cancellationToken); if (book is null) return NotFound();
        db.Books.Remove(book); await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Authors(string? search, CancellationToken cancellationToken)
    {
        var query = db.Authors.AsNoTracking().Include(author => author.Books).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(author => (author.Name + " " + author.Family).Contains(search));
        return Ok(await query.OrderBy(author => author.Family).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuthor(int id, CancellationToken cancellationToken) =>
        await db.Authors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken) is { } author ? Ok(author) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Author(Author author, CancellationToken cancellationToken)
    { author.Id = 0; db.Authors.Add(author); await db.SaveChangesAsync(cancellationToken); return Ok(author); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Author(int id, Author input, CancellationToken cancellationToken)
    { var author = await db.Authors.FindAsync([id], cancellationToken); if (author is null) return NotFound(); author.Name = input.Name; author.Family = input.Family; await db.SaveChangesAsync(cancellationToken); return NoContent(); }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Author(int id, CancellationToken cancellationToken)
    { var author = await db.Authors.Include(item => item.Books).SingleOrDefaultAsync(item => item.Id == id, cancellationToken); if (author is null) return NotFound(); if (author.Books.Count != 0) return Conflict(new { message = "ابتدا کتاب‌های نویسنده را منتقل یا حذف کنید." }); db.Authors.Remove(author); await db.SaveChangesAsync(cancellationToken); return NoContent(); }

    [HttpGet]
    public async Task<IActionResult> Categories(string? search, CancellationToken cancellationToken)
    { var query = db.BookCategories.AsNoTracking().Include(category => category.Books).AsQueryable(); if (!string.IsNullOrWhiteSpace(search)) query = query.Where(category => category.CategoryName.Contains(search)); return Ok(await query.OrderBy(category => category.CategoryName).ToListAsync(cancellationToken)); }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken) =>
        await db.BookCategories.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken) is { } category ? Ok(category) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Category(BookCat category, CancellationToken cancellationToken)
    { category.Id = 0; db.BookCategories.Add(category); await db.SaveChangesAsync(cancellationToken); return Ok(category); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Category(int id, BookCat input, CancellationToken cancellationToken)
    { var category = await db.BookCategories.FindAsync([id], cancellationToken); if (category is null) return NotFound(); category.CategoryName = input.CategoryName; await db.SaveChangesAsync(cancellationToken); return NoContent(); }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Category(int id, CancellationToken cancellationToken)
    { var category = await db.BookCategories.Include(item => item.Books).SingleOrDefaultAsync(item => item.Id == id, cancellationToken); if (category is null) return NotFound(); if (category.Books.Count != 0) return Conflict(new { message = "دسته دارای کتاب است." }); db.BookCategories.Remove(category); await db.SaveChangesAsync(cancellationToken); return NoContent(); }
}
