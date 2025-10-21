using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true, Required = AuthTokenKind.Guest | AuthTokenKind.Jwt)]
public sealed class ElementsController(ElementsRepository elements, IBoardsRepository boards) : BaseController
{
    [HttpGet("/api/boards/{boardId}/elements")]
    public async Task<IActionResult> GetBoardElements(Guid boardId)
    {
        if (!await CanAccessBoard(boardId, HttpContext.AuthUser, HttpContext.GuestToken, HttpContext.RequestAborted))
        {
            return Forbid();
        }

        var rows = await elements.GetElementsWithStickersAsync(boardId, HttpContext.RequestAborted);

        var response = rows.Select(r => new ElementDto
        {
            Id = r.Id,
            Type = r.Type,
            X = r.X,
            Y = r.Y,
            Width = r.Width,
            Height = r.Height,
            ZIndex = r.ZIndex,
            Rotation = r.Rotation,
            Text = r.Text,
            Color = r.Color,
            FontSize = r.FontSize
        }).ToList();
        
        return Ok(response);
    }

    [HttpPost("/api/boards/{boardId}/elements/bulk")]
    public async Task<IActionResult> UpsertBoardElements(Guid boardId, BulkElementsRequest dto)
    {
        if (!await CanAccessBoard(boardId, HttpContext.AuthUser, HttpContext.GuestToken, HttpContext.RequestAborted))
        {
            return Forbid();
        }

        if (dto.Elements.Count == 0)
        {
            return BadRequest(message:"no_elements");
        }

        var items = dto.Elements.Select(e => new BoardElement
        {
            Id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
            BoardId = boardId,
            UserId = HttpContext.AuthUser?.Id,
            Type = e.Type,
            X = e.X,
            Y = e.Y,
            Width = e.Width,
            Height = e.Height,
            ZIndex = e.ZIndex,
            Rotation = e.Rotation,
            Text = e.Text,
            Color = e.Color,
            FontSize = e.FontSize
        }).ToList();

        await elements.SyncBoardElementsAsync(boardId, HttpContext.AuthUser?.Id, items, HttpContext.RequestAborted);

        return Ok(new
        {
            updated = items.Count
        });
    }
    
    
    private async Task<bool> CanAccessBoard(Guid boardId, User? user, string? guestKey, CancellationToken cancel = default)
    {
        var meta = await boards.GetBoardMetaAsync(boardId, cancel);
        if (meta is null)
        {
            Console.WriteLine($"[ACCESS] board {boardId} not found");
            return false;
        }

        if (user?.Id != null && meta.Value.OwnerUserId == user.Id)
        {
            Console.WriteLine($"[ACCESS] board {boardId} allowed by owner {user.Id}");
            return true;
        }

        if (meta.Value is not { IsTemporary: true, TempKey: { } tk, TempExpiresAt: { } exp })
        {
            Console.WriteLine($"[ACCESS] board {boardId} not temporary");
            return false;
        }

        Console.WriteLine($"[ACCESS] guestKey={guestKey}, expected={tk}, exp={exp}, now={DateTime.UtcNow}");

        if (!Guid.TryParse(guestKey, out var gk))
            return false;

        if (gk == tk && exp < DateTime.UtcNow)
        {
            await boards.DeleteExpiredGuestBoardsAsync(cancel);
            return false;
        }

        return !string.IsNullOrEmpty(guestKey) && gk == tk && exp > DateTime.UtcNow;
    }
}