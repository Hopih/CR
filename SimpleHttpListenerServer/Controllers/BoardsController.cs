using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(Required = AuthTokenKind.Jwt)]
public sealed class BoardsController
{
    private readonly BoardsRepository _boards;

    public BoardsController(BoardsRepository boards)
    {
        _boards = boards;
    }

    [HttpGet("/api/boards")]
    public async Task<object> GetUserBoards(HttpContextEx ctx, CancellationToken cancel = default)
    {
        var user = ctx.AuthUser!;
        var boards = await _boards.GetUserBoardsAsync(user.Id, cancel);
        return boards.Select(b => new { b.Id, b.Title }).ToList();
    }

    [HttpPost("/api/boards")]
    public async Task<object> CreateUserBoard(CreateBoardRequest dto, HttpContextEx ctx, CancellationToken cancel = default)
    {
        var user = ctx.AuthUser!;
        var id = await _boards.CreateUserBoardAsync(user.Id, dto.Title, cancel);
        ctx.Response.StatusCode = 201;
        return new { id };
    }
}