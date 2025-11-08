using System.Net;
using System.Text;
using System.Text.Json;
using CacheExample;
using StackExchange.Redis;
using WebServerExample.Models;

namespace WebServerExample;

public static class WebHelper
{
    private static readonly DatabaseContext DbContext = new ("localhost\\SQLEXPRESS", "MySiteExampleDb");
    
    [Mapping("weather","GET", "user")]
    public static async Task ShowWeatherApi(HttpListenerContext httpListenerContext)
    {
        var sessionId = httpListenerContext.Request.Cookies["session-id"];
        if (sessionId != null)
        {
            var userId = RedisStore.RedisCache.StringGet(sessionId.Value);
            if (userId.HasValue)
            {
                //todo api add
            }
        }
        httpListenerContext.Response.StatusCode = 403;
        httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("У вас нет доступа к этому функционалу"));
    }
    
    [Mapping("user","POST")]
    public static async Task SaveUser(HttpListenerContext httpListenerContext)
    {
        using var sr = new StreamReader(httpListenerContext.Request.InputStream);

        var user = JsonSerializer.Deserialize<User>(await sr.ReadToEndAsync());
        if (user != null)
        {
            var result = await DbContext.AddUser(user);

            if (result != null)
            {
                httpListenerContext.Response.StatusCode = 204;
                httpListenerContext.Response.ContentLength64 = 0;
                return;
            }
        }
        httpListenerContext.Response.StatusCode = 500;
        httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
        httpListenerContext.Response.OutputStream.Write(Encoding.UTF8.GetBytes("Передача данных на сервер не удалась!"));
    }

    [Mapping("signin", "POST")]
    public static async Task Login(HttpListenerContext context)
    {
        using var sr = new StreamReader(context.Request.InputStream);

        var loginModel = JsonSerializer.Deserialize<LoginModel>(await sr.ReadToEndAsync());
        var user = await DbContext.GetUser(loginModel);
        if (user != null)
        {
            var sessionId = BCrypt.Net.BCrypt.HashPassword(user.Id + "salt");
            
            await RedisStore.RedisCache.SetAddAsync(new RedisKey(sessionId), new RedisValue(user.Id.ToString()));
            context.Response.Cookies.Add(new Cookie
            {
                Name = "session-id",
                Value = sessionId,
                Port = "5000",
                Expires = DateTime.UtcNow.AddMinutes(30d)
            });
            await ShowFile(@"public\index.html",context);
            return;
        }
        
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 401;
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Данный пользователь не найден!"));
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
    
    [Mapping("signup","GET")]
    public static async Task ShowSignUp(HttpListenerContext context)
    {
        await ShowFile(@"public\signup.html",context);
    }
    
    [Mapping("login","GET")]
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