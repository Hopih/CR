using System.Net;
using HttpListenerStepByStep;
using HttpListenerStepByStep.Http;

var origin = Environment.GetEnvironmentVariable("BOARD_ORIGIN") 
             ?? "http://localhost:5000";

using var httpListener = new HttpListener();
using var quotes = new QuotesApiClient();
var router = RouterExtensions.CreateRouterAndMap(quotes);

var processor = new RequestProcessor(router,
    new StaticFileHandler(Path.Combine(AppContext.BaseDirectory, "public")));

using var listener = new HttpListener();

listener.Prefixes.Add(origin.TrimEnd('/') + "/");
listener.Start();

Console.WriteLine(origin);

while (listener.IsListening)
{
    var context = await listener.GetContextAsync();
    _ = Task.Run(() => processor.HandleAsync(context, CancellationToken.None));
}

