using FluentValidation;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Http;

public sealed class BoardApiHandler(
    BoardRepository boards,
    StickerRepository stickers,
    IValidator<CreateBoardRequest> boardValidator,
    IValidator<SaveStickerRequest> stickerValidator)
{
    public async Task CreateAsync(RouteContext route, CancellationToken ct)
    {
        var input = await route.Http.Request.ReadAsync<CreateBoardRequest>(ct);
        
        input.Validate(boardValidator);
        
        var id = await boards.CreateAsync(input.Title!, ct);
        var board = await boards.GetAsync(id, ct) ?? throw Missing();
        
        route.Http.Response.Headers["Location"] = $"/api/boards/{id}";
        
        await route.Http.Response.WriteAsync(201,
            new BoardResponse(board.Id, board.Title, board.CreatedAt, []), ct);
    }

    public async Task GetAsync(RouteContext route, CancellationToken ct)
    {
        var id = route.Id("boardId");
        
        var board = await boards.GetAsync(id, ct) ?? throw Missing();
        
        var items = await stickers.GetByBoardAsync(id, ct);
        
        await route.Http.Response.WriteAsync(200,
            new BoardResponse(board.Id, board.Title, board.CreatedAt, items), ct);
    }

    public async Task DeleteAsync(RouteContext route, CancellationToken ct)
    {
        if (!await boards.DeleteAsync(route.Id("boardId"), ct))
        {
            throw Missing();
        }

        await route.Http.Response.WriteAsync(204, ct: ct);
    }

    public async Task CreateStickerAsync(RouteContext route, CancellationToken ct)
    {
        var boardId = route.Id("boardId");
        var input = await route.Http.Request.ReadAsync<SaveStickerRequest>(ct);
        input.Validate(stickerValidator);
        
        if (await boards.GetAsync(boardId, ct) is null)
        {
            throw Missing();
        }

        var id = await stickers.CreateAsync(boardId, input, ct);
        var items = await stickers.GetByBoardAsync(boardId, ct);
        var created = items.FirstOrDefault(x => x.Id == id) ?? throw Missing();
        
        route.Http.Response.Headers["Location"] = $"/api/boards/{boardId}/stickers/{id}";
        
        await route.Http.Response.WriteAsync(201, created, ct);
    }

    public async Task UpdateStickerAsync(RouteContext route, CancellationToken ct)
    {
        var input = await route.Http.Request.ReadAsync<SaveStickerRequest>(ct);
        
        input.Validate(stickerValidator);
        if (!await stickers.UpdateAsync(route.Id("boardId"), route.Id("stickerId"), 
                input, ct))
        {
            throw Missing();
        }

        await route.Http.Response.WriteAsync(204, ct: ct);
    }

    public async Task DeleteStickerAsync(RouteContext route, CancellationToken ct)
    {
        if (!await stickers.DeleteAsync(route.Id("boardId"), 
                route.Id("stickerId"), ct))
        {
            throw Missing();
        }

        await route.Http.Response.WriteAsync(204, ct: ct);
    }

    private static ApiException Missing() => 
        new(404, "not_found", "Объект не найден");
}