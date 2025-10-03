using System.Net;
using System.Text;
using System.Text.Json;
using CacheExample;
using CodeJam;
using HttpListenerExample.Entities;
using HttpListenerExample.Helpers;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using OpenApiHttpClientExample;
using StackExchange.Redis;

namespace HttpListenerExample;

public static class WebHelper
{
    
    public static async Task ShowWeatherApi(HttpListenerContext httpListenerContext, CancellationToken ctx)
    {
        //тут можно например счетчик вести для данного пользователя. Нас попросили просто вывести данные 
        var weatherApi = new OpenWeatherHttpClient();
        var weather = await weatherApi.GetWeatherDataByCity("Kazan");
        if (weather != null)
        {
            httpListenerContext.Response.StatusCode = 200;
            httpListenerContext.Response.ContentType = "application/json; charset=utf-8";
            await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(weather), ctx);
            return;
        }
        
        httpListenerContext.Response.StatusCode = 500;
        httpListenerContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpListenerContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Искомых данных о погоде нет!"), ctx);
    }

    public static async Task ShowStatic(HttpListenerContext context, CancellationToken ctx)
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
            await ShowFile(basePath, context, ctx);
        }
        else
        {
            await Show404(context, ctx);
        }
    }
    
    private static async Task Show404(HttpListenerContext context, CancellationToken ctx)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 404;
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Нужная вам страница не найдена!"), ctx);
    }

    private static async Task Show500(HttpListenerContext context, Exception ex, CancellationToken ctx)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 500;
        await context.Response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes(
                $"Произошла ошибка на стороне сервера. Информация по ошибке: {ex.ToDiagnosticString()}"), ctx);
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
                var userValidator = new UserValidator();

                var validationResult = await userValidator.ValidateAsync(user, ctx);

                if (validationResult is { IsValid: false })
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.StatusCode = 400;
                    await context.Response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes($"Ваши данные не прошли проверку! Информация по ошибке: " +
                                               $"{string.Join("\n\r", validationResult.Errors.Select(er => er.ErrorMessage))}"),
                        ctx);
                }

                //хешируем пароль
                user.Password = MyPasswordHasher.Hash(user.Password);

                var dbContext = new DbContext();
                await dbContext.AddUserAsync(user, ctx);

                //Про то как читать данные читайте тут https://metanit.com/sharp/mongodb/1.7.php
                var mongoDb = MongoDb.Client.GetDatabase("my-site");
                var usersCollection = mongoDb.GetCollection<User>("users");
                await usersCollection.InsertOneAsync(user,null, ctx);
                
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.StatusCode = 200;
                await context.Response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes("Поздравляем!"), ctx);
            }
        }
        catch(Exception ex)
        {
            await Show500(context, ex, ctx);
        }
    }

    public static async Task ShowIndex(HttpListenerContext context, CancellationToken ctx)
    {
        await ShowFile(@"public\index.html",context, ctx);
    }
    
    public static async Task ShowSignUp(HttpListenerContext context, CancellationToken ctx)
    {
        await ShowFile(@"public\signup.html",context, ctx);
    }
    
    public static async Task ShowLogin(HttpListenerContext context, CancellationToken ctx)
    {
        await ShowFile(@"public\login.html",context, ctx);
    }

    private static async Task ShowFile(string path, HttpListenerContext context, CancellationToken ctx)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = Path.GetExtension(path) switch
        {
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".html" => "text/html; charset=utf-8",
            _ => "text/plain; charset=utf-8"
        };
        var file = await File.ReadAllBytesAsync(path, ctx);
        await context.Response.OutputStream.WriteAsync(file, ctx);
    }

    public static async Task ShowSignIn(HttpListenerContext context, CancellationToken ctxToken)
    {
        using var sr = new StreamReader(context.Request.InputStream);
        var userLoginModel = JsonSerializer.Deserialize<UserLoginModel>(
            await sr.ReadToEndAsync().ConfigureAwait(false), 
            new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var dbContext = new DbContext();
        try
        {
            var user = await dbContext.GetUserByEmailAsync(
                userLoginModel!.Login, ctxToken);
            var sessionId = Guid.NewGuid().ToString();
            //альтернативное использование кеша в памяти
            //MemoryCache.Cache.Set(sessionId, user);
            await RedisStore.RedisCache.SetAddAsync(new RedisKey(sessionId), new RedisValue(user.Id.ToString()));

            if (MyPasswordHasher.Validate(user.Password, userLoginModel.Password))
            {
                context.Response.SetCookie(new Cookie
                {
                    Name = "session-id",
                    Value = sessionId,
                    Expires = DateTime.UtcNow.AddMinutes(30d)
                });
            }
        }
        catch (Exception ex)
        {
            await Show500(context, ex, ctxToken);
        }
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = 200;
        await context.Response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes("Поздравляем!"), ctxToken);
    }
}