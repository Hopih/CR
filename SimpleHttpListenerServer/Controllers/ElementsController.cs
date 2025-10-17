using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true, Required = AuthTokenKind.Guest | AuthTokenKind.Jwt)]
public sealed class ElementsController(ElementsRepository elements, BoardsRepository boards)
{
    [HttpGet("/api/boards/{boardId}/elements")]
    public async Task<object> GetBoardElements(Guid boardId, HttpContextEx ctx, CancellationToken cancel = default)
    {
        var guestKey = ctx.Request.Cookies?["guest_board_key"]?.Value ?? ctx.Request.Headers["X-Guest-Key"];
        if (!await CanAccessBoard(boardId, ctx.AuthUser, guestKey, cancel))
        {
            ctx.Response.StatusCode = 403;
            return new { error = "forbidden" };
        }

        var rows = await elements.GetElementsWithStickersAsync(boardId, cancel);
        return rows.Select(r => new ElementDto
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
    }

    [HttpPost("/api/boards/{boardId}/elements/bulk")]
    public async Task<object> UpsertBoardElements(Guid boardId, BulkElementsRequest dto, HttpContextEx ctx, CancellationToken cancel = default)
    {
        var guestKey = ctx.Request.Cookies?["guest_board_key"]?.Value ?? ctx.Request.Headers["X-Guest-Key"];
        if (!await CanAccessBoard(boardId, ctx.AuthUser, guestKey, cancel))
        {
            ctx.Response.StatusCode = 403;
            return new { error = "forbidden" };
        }

        if (dto.Elements.Count == 0)
        {
            ctx.Response.StatusCode = 400;
            return new { error = "no_elements" };
        }

        var items = dto.Elements.Select(e => new BoardElement
        {
            Id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
            BoardId = boardId,
            UserId = ctx.AuthUser?.Id,
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

        await elements.SyncBoardElementsAsync(boardId, ctx.AuthUser?.Id, items, cancel);

        return new { updated = items.Count };
    }
    
    
    private async Task<bool> CanAccessBoard(Guid boardId, User? user, string? guestKey, CancellationToken cancel = default)
    {
        var meta = await boards.GetBoardMetaAsync(boardId, cancel);
        if (meta is null) return false;

        if (user?.Id != null && meta.Value.OwnerUserId == user.Id)
        {
            return true;
        }

        if (meta.Value is not { IsTemporary: true, TempKey: { } tk, TempExpiresAt: { } exp })
        {
            return false;
        }

        if (!Guid.TryParse(guestKey, out var gk))
        {
            return false;
        }

        if (gk == tk && exp < DateTime.UtcNow)
        {
            await boards.DeleteExpiredGuestBoardsAsync(cancel);
            return false;
        }

        return gk == tk && exp > DateTime.UtcNow;
    }
}