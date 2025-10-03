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
var key = Environment.GetEnvironmentVariable("MY_SUPERPUPER_KEY");
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
                var authResult = await Login(context, ctx.Token);
                if (authResult != null)
                {
                    await response.OutputStream.WriteAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(authResult, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })), ctx.Token);
                }
                break;
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
                await ShowResourceFile(context, ctx.Token);
                break;
            case"/quotes/random" when request.HttpMethod == "GET":
                /*
                var auth = await AuthorizationJWT(context, ctx.Token);
                if (auth == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = "text/html";
                    await context.Response.OutputStream.WriteAsync(
                        "Вы не авторизованы!"u8.ToArray(), ctx.Token);
                }
                */
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
                var authUser = await AuthorizationJWT(context, ctx.Token);
                Console.WriteLine(authUser?.Id);
                break;
            default:
                await ShowResourceFile(context, ctx.Token);
                break;
        }
        
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();


async Task<AuthResult?> Login(HttpListenerContext context, CancellationToken cancellationToken)
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
        return null;
    }
    
    var user = await dbContext.GetUser(userLoginModel!.Login, cancellationToken);
    if (user == null)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Не найден пользователь с логином {userLoginModel.Login}"),
            cancellationToken).ConfigureAwait(false);
        return null;
    }

    if (!MyPasswordHasher.Validate(user.Password, userLoginModel.Password))
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Пароль не верный!"),
            cancellationToken).ConfigureAwait(false);
        return null;
    }
    
    /*
    var sessionId = Guid.NewGuid().ToString();
    LocalCache.Cache.Add(sessionId, new CacheItem
    {
        UserId = user.Id,
        Role = user.Role
    });
    */
    
    //context.Response.Headers["Set-Cookie"] = $"id={sessionId};path=/; expires={DateTime.Now.AddHours(10)}";
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html";
    return new AuthResult
    {
        Token = JwtWorker.GenerateJwtToken(user)
    };
}


async Task ShowResourceFile(HttpListenerContext context, CancellationToken token)
{
    if (context.Request.Url is null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    // путь относительно public/
    var relativePath = context.Request.Url.LocalPath.TrimStart('/');
    if (string.IsNullOrEmpty(relativePath) || relativePath == "home")
        relativePath = "index.html"; // по умолчанию отдаём index.html
    

    var filePath = Path.Combine("public", relativePath);

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = 404;
        return;
    }

    string contentType = Path.GetExtension(filePath).ToLower() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css"  => "text/css",
        ".js"   => "application/javascript",
        ".svg"  => "image/svg+xml",
        ".ico"  => "image/x-icon",
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"  => "image/gif",
        _       => "application/octet-stream"
    };

    context.Response.StatusCode = 200;
    context.Response.ContentType = contentType;

    var fileBytes = await File.ReadAllBytesAsync(filePath, token);
    await context.Response.OutputStream.WriteAsync(fileBytes, token);

    context.Response.Close();
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
    var userValidationRulesExample = userValidatorRule.Validate(userLoginModel!);
    
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


async Task<User?> AuthorizationJWT(HttpListenerContext context, CancellationToken cancellationToken)
{
    var auth = context.Request.Headers["Authorization"];
    if (auth == null)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/html";
        context.Response.ContentEncoding = Encoding.UTF8;
        await context.Response.OutputStream.WriteAsync("Вы давно не использовали сайт, повторно войдите!"u8.ToArray(),
            cancellationToken).ConfigureAwait(false);
        return null;
    }
    
    var token = auth.Split()[1];
    var tokenValidationResult = JwtWorker.ValidateJwtToken(token);
    if (tokenValidationResult.isSuccess)
    {
        return tokenValidationResult.user;
    }

    context.Response.ContentType = "text/html";
    context.Response.ContentEncoding = Encoding.UTF8;
    
    await context.Response.OutputStream.WriteAsync(
        "Вы давно не использовали сайт, повторно войдите!"u8.ToArray(),
        cancellationToken).ConfigureAwait(false);
    
    return null;
}