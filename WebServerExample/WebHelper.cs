using System.Net;
using System.Text;
using Newtonsoft.Json;
using OpenApiHttpClientExample;
using WebServerExample.Models;

namespace WebServerExample;

public static class WebHelper
{
    [Mapping("weather","GET")]
    public static async Task ShowWeatherApi(HttpListenerContext httpListenerContext)
    {
        var weatherApi = new OpenWeatherHttpClient();
        var weather = await weatherApi.GetWeatherDataByCity("Kazan");
        if (weather != null)
        {
            httpListenerContext.Response.StatusCode = 200;
            httpListenerContext.Response.ContentType = "application/json";
            await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(weather));
        }
        else
        {
            httpListenerContext.Response.StatusCode = 500;
            httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
            await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Искомых данных о погоде нет!"));
        }
    }
    
    [Mapping("user","POST")]
    public static async Task ShowUserGuid(HttpListenerContext httpListenerContext)
    {
        using var sr = new StreamReader(httpListenerContext.Request.InputStream);

        var user = JsonConvert.DeserializeObject<User>(await sr.ReadToEndAsync());
        if (user != null)
        {
            var dbContext = new DatabaseContext("localhost\\SQLEXPRESS", "MySiteExampleDb");
            var result = await dbContext.AddUser(user);

            if (result != null)
            {
                httpListenerContext.Response.StatusCode = 200;
                httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
                await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(user.Id.ToString()));
                
                return;
            }
        }
        httpListenerContext.Response.StatusCode = 500;
        httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
        httpListenerContext.Response.OutputStream.Write(Encoding.UTF8.GetBytes("Передача данных на сервер не удалась!"));
    }
    
    [Mapping("*","*")]
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

    [Mapping("home","GET")]
    public static async Task ShowIndex(HttpListenerContext context)
    {
        await ShowFile(@"public\index.html",context);
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