using System.Net;
using Npgsql;

namespace HttpListenerStepByStep.Http;

public sealed class RequestProcessor(Router router, StaticFileHandler files)
{
    public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            if (await router.TryHandleAsync(context, ct))
            {
                return;
            }

            var path = context.Request.Url!.AbsolutePath;
            if (path == "/api" || path.StartsWith("/api/"))
            {
                throw new ApiException(404, "not_found", "Маршрут не найден");
            }

            await files.HandleAsync(context, ct);
        }
        catch (ApiException ex)
        {
            await ErrorAsync(context, ex.Status, ex.Code, ex.Message, ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505" &&
                                           ex.ConstraintName == "uq_users_login")
        {
            await ErrorAsync(context, 409, "login_taken", "Логин уже занят", ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "23503" &&
                                           ex.ConstraintName is "stickers_board_id_fkey" or "fk_boards_owner")
        {
            await ErrorAsync(context, 404, "not_found", "Связанный объект не найден", ct);
        }
        catch (PostgresException ex) when (ex.SqlState is "23514" or "23502" or "22001")
        {
            await ErrorAsync(context, 400, "validation_error", "Неверные данные", ct);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.GetType().Name);
            await ErrorAsync(context, 500, "server_error", "Ошибка сервера", ct);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static async Task ErrorAsync(HttpListenerContext context,
        int status, string code, string? message, CancellationToken ct)
    {
        try
        {
            await context.Response.WriteAsync(status, new { error = new { code, message } }, ct);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.GetType().Name);
        }
    }
}