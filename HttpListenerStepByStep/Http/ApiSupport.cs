using System.Net;
using System.Text.Json;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Http;

public static class ApiSupport
{
    public static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpListenerResponse response, int status,
        object? value = null, CancellationToken ct = default)
    {
        response.StatusCode = status;
        if (status == 204)
        {
            response.ContentLength64 = 0;
            return;
        }
        response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(response.OutputStream, value, Json, ct);
    }

    public static async Task<T> ReadAsync<T>(
        HttpListenerRequest request, CancellationToken ct)
    {
        var mediaType = request.ContentType?.Split(';')[0].Trim();
        if (!string.Equals(mediaType, "application/json",
                StringComparison.OrdinalIgnoreCase))
            throw new ApiException(415, "unsupported_media_type", "Нужен JSON");
        try
        {
            var value = await JsonSerializer.DeserializeAsync<T>(
                request.InputStream, Json, ct);
            return value is not null ? value
                : throw new ApiException(400, "invalid_json", "Пустое тело запроса");
        }
        catch (JsonException)
        {
            throw new ApiException(400, "invalid_json",
                "Неверный JSON или отсутствуют обязательные поля");
        }
    }

    public static Guid ParseId(string value)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new ApiException(400, "invalid_id", "Неверный UUID");
        return id;
    }

    public static void Validate(this CreateBoardRequest value)
    {
        if (string.IsNullOrWhiteSpace(value.Title) || value.Title.Length > 120)
            throw new ApiException(400, "validation_error",
                "Заголовок должен содержать от 1 до 120 символов");
    }

    public static void Validate(this SaveStickerRequest value)
    {
        if (value.Text is null || value.Text.Length > 2000 ||
            value.X is < 0 or > 10000 || value.Y is < 0 or > 10000 ||
            value.Width is < 80 or > 10000 || value.Height is < 80 or > 10000)
            throw new ApiException(400, "validation_error",
                "Проверьте текст, координаты и размеры стикера");
    }
}