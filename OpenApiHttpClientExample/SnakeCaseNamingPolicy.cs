using System.Text.Json;

namespace OpenApiHttpClientExample;

public class SnakeCaseNamingPolicy  : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToSnakeCase();
}