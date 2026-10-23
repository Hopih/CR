using System.Net;
using System.Text.Json;
using FluentValidation;

namespace HttpListenerStepByStep.Http;

public static class ApiSupport
{
    public static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        this HttpListenerResponse response, int status,
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
        this HttpListenerRequest request, CancellationToken ct)
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

    public static void Validate<T>(this T value, IValidator<T> validator)
    {
        var result = validator.Validate(value);
        if (!result.IsValid)
            throw new ApiException(400, "validation_error",
                string.Join("; ", result.Errors.Select(x => x.ErrorMessage).Distinct()));
    }
}