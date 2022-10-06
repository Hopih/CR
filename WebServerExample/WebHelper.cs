using System.Net;
using System.Text;
using CacheExample;
using Newtonsoft.Json;
using OpenApiHttpClientExample;
using StackExchange.Redis;
using WebServerExample.Models;

namespace WebServerExample;

public static class WebHelper
{
    static readonly DatabaseContext _dbContext = new ("localhost\\SQLEXPRESS", "MySiteExampleDb");
    
    [Mapping("weather","GET", "user")]
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
    public static async Task SaveUser(HttpListenerContext httpListenerContext)
    {
        using var sr = new StreamReader(httpListenerContext.Request.InputStream);

        var user = JsonConvert.DeserializeObject<User>(await sr.ReadToEndAsync());
        if (user != null)
        {
            var result = await _dbContext.AddUser(user);

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

    [Mapping("signin", "POST")]
    public static async Task Login(HttpListenerContext context)
    {
        using var sr = new StreamReader(context.Request.InputStream);

        var loginModel = JsonConvert.DeserializeObject<LoginModel>(await sr.ReadToEndAsync());
        var user = await _dbContext.GetUser(loginModel);
        if (user != null)
        {
            var sessionId = BCrypt.Net.BCrypt.HashPassword(user.Password + "salt");
            
            await RedisStore.RedisCache.SetAddAsync(new RedisKey(sessionId), new RedisValue(user.Id.ToString()));
            context.Response.Cookies.Add(new Cookie
            {
                Name = user.Email,
                Value = sessionId,
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