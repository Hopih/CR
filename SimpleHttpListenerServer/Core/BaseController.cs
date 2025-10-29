namespace SimpleHttpListenerServer.Core;

public abstract class BaseController
{
    public HttpContextEx HttpContext { get; internal set; } = default!;

    protected IActionResult Ok(object? value = null)
        => new JsonResult(value, 200);

    protected IActionResult Created(object? value = null)
        => new JsonResult(value, 201);

    protected IActionResult NoContent()
        => new JsonResult(null, 204);

    protected IActionResult BadRequest(object? value = null, string? message = null)
        => new JsonResult(value ?? new { error = message ?? "Bad Request" }, 400);

    protected IActionResult NotFound(object? value = null)
        => new JsonResult(value ?? new { error = "Not Found" }, 404);

    protected IActionResult Unauthorized(object? value = null)
        => new JsonResult(value ?? new { error = "Unauthorized" }, 401);
    
    protected IActionResult Conflict(string? errorMessage = null)
        => new JsonResult(new { error = errorMessage ?? "Conflict" }, 409);

    protected IActionResult Forbid(object? value = null)
        => new JsonResult(value ?? new { error = "Forbidden" }, 403);

    protected IActionResult Problem(string message, int status = 500)
        => new JsonResult(new { error = message }, status);
    
    protected IActionResult Json(object? value, int status = 200)
        => new JsonResult(value, status);
}

public interface IActionResult
{
    Task ExecuteAsync(HttpContextEx ctx, CancellationToken ct);
}