using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Npgsql.Replication;
using SimpleHttpListenerServer;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Models;

var httpListener = new HttpListener();
var dbContext = new DbContext();
httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();
var quotesApiClient = new QuotesApiClient();
while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    var response = context.Response;
    var request = context.Request;
    var ctx = new CancellationTokenSource();
    _ = Task.Run(async () =>
    {
        switch (request.Url?.LocalPath)
        {
            case"/user/login" when request.HttpMethod == "POST":
                
            case "/user/signup" when request.HttpMethod == "POST":
                var user = await SaveUser(context, ctx.Token);
                if (user == null)
                    break;
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })), ctx.Token);
                break;
            case "/home" when request.HttpMethod == "GET":
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                var file = await File.ReadAllBytesAsync("public/index.html", ctx.Token);
                await context.Response.OutputStream.WriteAsync(file, ctx.Token);
                break;
            case"/quotes/random" when request.HttpMethod == "GET":
                var quote = await quotesApiClient.GetRandomQuoteAsync(ctx.Token);
                response.StatusCode = 200;  
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quote, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })), ctx.Token);
                break;
            case "/board" when request.HttpMethod == "GET":
                
            default:
                await ShowResourseFile(context, ctx.Token);
                break;
        }
        
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();


async Task Login(HttpListenerContext context, CancellationToken cancellationToken)
{
    using var sr = new StreamReader(context.Request.InputStream);
    var userLoginModel = JsonSerializer.Deserialize<UserLoginModel>(
        await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false), 
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    if (userLoginModel == null)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync("Не переданы данные логина или пароля!"u8.ToArray(),
            cancellationToken).ConfigureAwait(false);
        return;
    }
    
    var user = await dbContext.GetUser(userLoginModel!.Login, cancellationToken);
    if (user == null)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Не найден пользователь с логином {userLoginModel.Login}"),
            cancellationToken).ConfigureAwait(false);
        return;
    }

    if (!MyPasswordHasher.Validate(user.Password, userLoginModel.Password))
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Пароль не верный!"),
            cancellationToken).ConfigureAwait(false);
        return;
    }

    var sessionId = Guid.NewGuid().ToString();
    LocalCache.Cache.Add(sessionId, new CacheItem
    {
        UserId = user.Id,
        Role = user.Role
    });
    context.Response.Headers["Set-Cookie"] = $"id={sessionId};path=/; expires={DateTime.Now.AddHours(10)}";
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html";
}


async Task ShowResourseFile(HttpListenerContext context, CancellationToken token)
{
    if (context.Request.Url is null)
    {
        context.Response.StatusCode = 404;
        return;
    }
    var path = context.Request.Url?.LocalPath.Split('/')[^1];
    
    
    context.Response.StatusCode = 200;
    context.Response.ContentType = path!.Split('.')[^1] switch
    {
        "html" => "text/html",
        "css" => "text/css",
        "js" => "text/javascript",
        "svg" => "image/svg+xml",
        "ico" => "image/x-icon",
        _ => throw new ArgumentOutOfRangeException()
    };
    
    var file = await File.ReadAllBytesAsync($"public/{path}", token);
    await context.Response.OutputStream.WriteAsync(file, token);
}

async Task<User?> SaveUser(HttpListenerContext context, CancellationToken token)
{
    using var sr = new StreamReader(context.Request.InputStream);
    var userLoginModel = JsonSerializer.Deserialize<User>(
        await sr.ReadToEndAsync(token).ConfigureAwait(false), 
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    //var userValidator = new UserValidator();
    var userValidatorRule = new UserValidationRules();
    var userValidationRulesExample = userValidatorRule.Validate(userLoginModel);
    //var validationResult = userValidator.Validate(new ValidationContext(userLoginModel)).ToList(); 
    if (!userValidationRulesExample.IsValid)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        context.Response.ContentEncoding = Encoding.UTF8;
        /*
        await context.Response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes($"Ваши данные не прошли проверку! Информация по ошибке: " +
                                   $"{string.Join("\n\r", validationResult.Select(er => er.ErrorMessage))}"),
            token);
        */
        await context.Response.OutputStream.WriteAsync(
            Encoding.UTF8.GetBytes($"Ваши данные не прошли проверку! Информация по ошибке: " +
                                   $"{string.Join("\n\r", userValidationRulesExample.Errors.Select(er => er.ErrorMessage))}"),
            token);
        return null;
    }
    
    userLoginModel!.Password = MyPasswordHasher.Hash(userLoginModel.Password);
    
    return await dbContext.CreateUser(userLoginModel!.Login, userLoginModel.Password, token);
}

async Task<CacheItem?> Authorization(HttpListenerContext context, CancellationToken cancellationToken)
{
    var cookie = context.Request.Headers["Cookie"];
    if (cookie == null)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/html";
        context.Response.ContentEncoding = Encoding.UTF8;
        await context.Response.OutputStream.WriteAsync("Вы давно не использовали сайт, повторно войдите!"u8.ToArray(),
            cancellationToken).ConfigureAwait(false);
        return null;
    }
    
    var session = cookie.Split(';')
        .FirstOrDefault(x => x.Contains("id="))?.Split('=');
    if (session != null && LocalCache.Cache.TryGetValue(session[1], out var cacheItem))
    {
        return cacheItem;
    }

    context.Response.ContentType = "text/html";
    context.Response.ContentEncoding = Encoding.UTF8;
    
    await context.Response.OutputStream.WriteAsync(
        "Вы давно не использовали сайт, повторно войдите!"u8.ToArray(),
        cancellationToken).ConfigureAwait(false);
    
    return null;
}