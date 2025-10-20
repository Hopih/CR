using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(Required = AuthTokenKind.Jwt)]
public sealed class BoardsController(BoardsRepository boards) : BaseController
{
    [HttpGet("/api/boards")]
    public async Task<IActionResult> GetUserBoards()
    {
        var user = HttpContext.AuthUser!;
        var boardsResult = await boards.GetUserBoardsAsync(user.Id, HttpContext.RequestAborted);
        
        return Ok(
            boardsResult.Select(b => 
                new
                {
                    b.Id, b.Title
                }).ToList()
            );
    }

    [HttpPost("/api/boards")]
    public async Task<IActionResult> CreateUserBoard(CreateBoardRequest dto)
    {
        var user = HttpContext.AuthUser!;
        var id = await boards.CreateUserBoardAsync(user.Id, dto.Title, HttpContext.RequestAborted);

        return Created(new { id });
    }
}