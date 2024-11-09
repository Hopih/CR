using System.Net;
using System.Text;
using System.Text.Json;
using SimpleHttpListenerServer;

var httpListener = new HttpListener();
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
        _ => throw new ArgumentOutOfRangeException()
    };
    
    var file = await File.ReadAllBytesAsync($"public/{path}", token);
    await context.Response.OutputStream.WriteAsync(file, token);
}