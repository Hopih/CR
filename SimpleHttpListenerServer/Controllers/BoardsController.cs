using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(Required = AuthTokenKind.Jwt)]
public sealed class BoardsController(BoardsRepository boards)
{
    [HttpGet("/api/boards")]
    public async Task<object> GetUserBoards(HttpContextEx ctx)
    {
        var user = ctx.AuthUser!;
        var boards1 = await boards.GetUserBoardsAsync(user.Id, ctx.RequestAborted);
        return boards1.Select(b => new { b.Id, b.Title }).ToList();
    }

    [HttpPost("/api/boards")]
    public async Task<object> CreateUserBoard(CreateBoardRequest dto, HttpContextEx ctx)
    {
        var user = ctx.AuthUser!;
        var id = await boards.CreateUserBoardAsync(user.Id, dto.Title, ctx.RequestAborted);
        ctx.Response.StatusCode = 201;
        return new { id };
    }
}