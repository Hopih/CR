using System.Net;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Models;
using Npgsql;

namespace HttpListenerStepByStep.Http;

public sealed class BoardApiHandler(
    BoardRepository boards, StickerRepository stickers)
{
    public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            await RouteAsync(context, ct);
        }
        catch (ApiException ex)
        {
            await ErrorAsync(context, ex.Status, ex.Code, ex.Message, ct);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
            ex.ConstraintName == "stickers_board_id_fkey")
        {
            await ErrorAsync(context, 404, "board_not_found", "Доска не найдена", ct);
        }
        catch (PostgresException ex) when (
            ex.SqlState is PostgresErrorCodes.CheckViolation
                or PostgresErrorCodes.NotNullViolation
                or PostgresErrorCodes.StringDataRightTruncation)
        {
            await ErrorAsync(context, 400, "validation_error", "Неверные данные", ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            await ErrorAsync(context, 500, "server_error", "Ошибка сервера", ct);
        }
    }

    private async Task RouteAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;
        var parts = (request.Url?.AbsolutePath ?? "")
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || parts[0] != "api" || parts[1] != "boards")
            throw Missing();
        var method = request.HttpMethod;

        if (parts.Length == 2)
        {
            Allow(context, "POST");
            var input = await ApiSupport.ReadAsync<CreateBoardRequest>(request, ct);
            input.Validate();
            var id = await boards.CreateAsync(input.Title, ct);
            // Читаем серверные поля, в том числе createdAt.
            var board = await boards.GetAsync(id, ct) ?? throw Missing();
            response.Headers["Location"] = $"/api/boards/{id}";
            await ApiSupport.WriteAsync(response, 201, new BoardResponse(
                board.Id, board.Title, board.CreatedAt, Array.Empty<Sticker>()), ct);
            return;
        }

        if (parts.Length > 5 || (parts.Length >= 4 && parts[3] != "stickers"))
            throw Missing();
        var boardId = ApiSupport.ParseId(parts[2]);

        if (parts.Length == 3)
        {
            Allow(context, "GET", "DELETE");
            if (method == "GET")
            {
                var board = await boards.GetAsync(boardId, ct) ?? throw Missing();
                var items = await stickers.GetByBoardAsync(boardId, ct);
                await ApiSupport.WriteAsync(response, 200, new BoardResponse(
                    board.Id, board.Title, board.CreatedAt, items), ct);
            }
            else
            {
                if (!await boards.DeleteAsync(boardId, ct)) throw Missing();
                await ApiSupport.WriteAsync(response, 204, ct: ct);
            }
            return;
        }

        if (parts.Length == 4)
        {
            Allow(context, "POST");
            var input = await ApiSupport.ReadAsync<SaveStickerRequest>(request, ct);
            input.Validate();
            if (await boards.GetAsync(boardId, ct) is null) throw Missing();
            var id = await stickers.CreateAsync(boardId, input, ct);
            response.Headers["Location"] = $"/api/boards/{boardId}/stickers/{id}";
            // Для простоты читаем созданный объект через существующую функцию.
            var items = await stickers.GetByBoardAsync(boardId, ct);
            var created = items.FirstOrDefault(x => x.Id == id) ?? throw Missing();
            await ApiSupport.WriteAsync(response, 201, created, ct);
            return;
        }

        var stickerId = ApiSupport.ParseId(parts[4]);
        Allow(context, "PUT", "DELETE");
        bool found;
        if (method == "PUT")
        {
            var input = await ApiSupport.ReadAsync<SaveStickerRequest>(request, ct);
            input.Validate();
            found = await stickers.UpdateAsync(boardId, stickerId, input, ct);
        }
        else
        {
            found = await stickers.DeleteAsync(boardId, stickerId, ct);
        }
        if (!found) throw Missing();
        await ApiSupport.WriteAsync(response, 204, ct: ct);
    }

    private static void Allow(HttpListenerContext context, params string[] methods)
    {
        if (methods.Contains(context.Request.HttpMethod)) return;
        context.Response.Headers["Allow"] = string.Join(", ", methods);
        throw new ApiException(405, "method_not_allowed", "Метод не поддерживается");
    }

    private static ApiException Missing() =>
        new(404, "not_found", "Доска, стикер или маршрут не найдены");

    private static Task ErrorAsync(
        HttpListenerContext context, int status,
        string code, string message, CancellationToken ct) =>
        ApiSupport.WriteAsync(context.Response, status,
            new { error = new { code, message } }, ct);
}