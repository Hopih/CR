using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class GuestBoardsController(BoardsRepository boards)
{
    [HttpPost("/api/guest/board")]
    public async Task<object> CreateGuestBoard(CreateBoardRequest dto, HttpContextEx ctx)
    {
        
        var title = string.IsNullOrWhiteSpace(dto.Title)
            ? "Guest board"
            : dto.Title.Trim();
        
        var (boardId, tempKey, expires) = await boards.CreateGuestBoardAsync(title, TimeSpan.FromDays(3), ctx.RequestAborted);

        ctx.Response.AddHeader("Set-Cookie",
            $"guest_board_key={tempKey}; Path=/; Max-Age={3 * 24 * 3600}; HttpOnly; SameSite=None; Secure");

        ctx.Response.StatusCode = 201;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return new CreateGuestBoardResponse
        {
            BoardId = boardId,
            TempKey = tempKey,
            ExpiresAt = expires
        };
    }
}
