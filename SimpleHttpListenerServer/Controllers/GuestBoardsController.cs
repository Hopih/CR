using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class GuestBoardsController(BoardsRepository boards) : BaseController
{
    [HttpPost("/api/guest/board")]
    public async Task<IActionResult> CreateGuestBoard(CreateBoardRequest dto) 
    {
        
        var title = string.IsNullOrWhiteSpace(dto.Title)
            ? "Guest board"
            : dto.Title.Trim();
        
        var (boardId, tempKey, expires) = await boards.CreateGuestBoardAsync(title, TimeSpan.FromDays(3), HttpContext.RequestAborted);

        HttpContext.Response.AddHeader("Set-Cookie",
            $"guest_board_key={tempKey}; Path=/; Max-Age={3 * 24 * 3600}; HttpOnly; SameSite=None; Secure");
        
        return Created(new
        {
            boardId,
            tempKey,
            expiresAt = expires
        });
    }
}
