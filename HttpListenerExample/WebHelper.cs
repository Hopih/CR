using System.Net;
using System.Text;
using System.Text.Json;
using CodeJam;
using HttpListenerExample;
using HttpListenerExample.Entities;
using OpenApiHttpClientExample;

namespace WebServerExample;

public static class WebHelper
{
    
    public static async Task ShowWeatherApi(HttpListenerContext httpListenerContext)
    {
        //тут можно например счетчик вести для данного пользователя. Нас попросили просто вывести данные 
        var weatherApi = new OpenWeatherHttpClient();
        var weather = await weatherApi.GetWeatherDataByCity("Kazan");
        if (weather != null)
        {
            httpListenerContext.Response.StatusCode = 200;
            httpListenerContext.Response.ContentType = "application/json";
            await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(weather));
            return;
        }
        
        httpListenerContext.Response.StatusCode = 500;
        httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Искомых данных о погоде нет!"));
    }

    public static async Task ShowStatic(HttpListenerContext context)
    {
        var path = context.Request.Url?.LocalPath
            .Split("/")
            .Skip(1)
            .ToArray();
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "public");

        if (path != null)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                basePath = Path.Combine(basePath, $@"{path[i]}\");
            }
        }

        basePath = Path.Combine(basePath, path?[^1] ?? string.Empty);
    
        if (File.Exists(basePath))
        {
            await ShowFile(basePath, context);
        }
        else
        {
            await Show404(context);
        }
    }
    
    private static async Task Show404(HttpListenerContext context)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 404;
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Нужная вам страница не найдена!"));
    }

    private static async Task Show500(HttpListenerContext context, Exception ex)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 500;
        await context.Response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(
                $"Произошла ошибка на стороне сервера. Информация по ошибке: {ex.ToDiagnosticString()}"));
    }

    public static async Task AddUserAsync(HttpListenerContext context, CancellationToken ctx)
    {

        try
        {
            using var sr = new StreamReader(context.Request.InputStream);
            var userStr = await sr.ReadToEndAsync().ConfigureAwait(false);
            var user = JsonSerializer.Deserialize<User>(userStr, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (user != null)
            {
                var dbContext = new DbContext();
                await dbContext.AddUserAsync(user, ctx);
                await context.Response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes("Поздравляем!"), ctx);
            }
        }
        catch(Exception ex)
        {
            await Show500(context, ex);
        }
    }

    public static async Task ShowIndex(HttpListenerContext context)
    {
        await ShowFile(@"public\index.html",context);
    }
    
    public static async Task ShowSignUp(HttpListenerContext context)
    {
        await ShowFile(@"public\signup.html",context);
    }
    
    public static async Task ShowLogin(HttpListenerContext context)
    {
        await ShowFile(@"public\login.html",context);
    }

    private static async Task ShowFile(string path, HttpListenerContext context)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = Path.GetExtension(path) switch
        {
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".html" => "text/html",
            _ => "text/plain"
        };
        var file = await File.ReadAllBytesAsync(path);
        await context.Response.OutputStream.WriteAsync(file);
    }
}