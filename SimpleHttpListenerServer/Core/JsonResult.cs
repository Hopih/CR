using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Core;

public sealed class JsonResult(object? value, int statusCode = 200) : IActionResult
{
    public async Task ExecuteAsync(HttpContextEx ctx, CancellationToken ct)
    {
        await ctx.Response.WriteJsonAsync(value, statusCode, ct);
    }
}