using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;
using webapicore.Services;

namespace webapicore.Controllers;

[ApiController, ApiKeyAuth, Authorize, Route("api/[controller]/[action]")]
public sealed class EngagementController(IBookEngagementService engagement) : ControllerBase
{
    [HttpGet("{bookId:int}")]
    public async Task<ActionResult<BookEngagementDto>> Book(int bookId, CancellationToken cancellationToken) =>
        Ok(await engagement.GetAsync(UserId(), bookId, cancellationToken));

    [HttpPut("{bookId:int}")]
    public async Task<ActionResult<BookEngagementDto>> Rating(int bookId, RatingRequest request, CancellationToken cancellationToken) =>
        Ok(await engagement.RateAsync(UserId(), bookId, request.Value, cancellationToken));

    [HttpDelete("{bookId:int}")]
    public async Task<ActionResult<BookEngagementDto>> Rating(int bookId, CancellationToken cancellationToken) =>
        Ok(await engagement.RateAsync(UserId(), bookId, null, cancellationToken));

    [HttpPut("{bookId:int}/{liked:bool}")]
    public async Task<ActionResult<BookEngagementDto>> Like(int bookId, bool liked, CancellationToken cancellationToken) =>
        Ok(await engagement.LikeAsync(UserId(), bookId, liked, cancellationToken));

    [HttpPost("{bookId:int}")]
    public async Task<IActionResult> LibraryEvent(int bookId, LibraryEventRequest request, CancellationToken cancellationToken)
    {
        await engagement.RecordLibraryEventAsync(UserId(), bookId, request.EventType, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Library(CancellationToken cancellationToken) =>
        Ok(await engagement.GetLibraryAsync(UserId(), cancellationToken));

    [HttpDelete("{bookId:int}")]
    public async Task<IActionResult> Library(int bookId, CancellationToken cancellationToken)
    {
        await engagement.RemoveFromLibraryAsync(UserId(), bookId, cancellationToken);
        return NoContent();
    }

    private int UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
