using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class GuestBoardsController(BoardsRepository boards)
{
    [HttpPost("/api/guest/board")]
    public async Task<object> CreateGuestBoard(CreateBoardRequest dto, HttpContextEx ctx, CancellationToken cancel = default)
    {
        var (boardId, tempKey, expires) = await boards.CreateGuestBoardAsync(dto.Title ?? "Guest board", TimeSpan.FromDays(3), cancel);

        ctx.Response.AddHeader("Set-Cookie", $"guest_board_key={tempKey}; Path=/; Max-Age={ 3 * 24 * 3600 }; HttpOnly");

        ctx.Response.StatusCode = 201;
        return new CreateGuestBoardResponse
        {
            BoardId = boardId,
            TempKey = tempKey,
            ExpiresAt = expires
        };
    }
}
