using SimpleHttpListenerServer.Core;

const string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=white_board";

var maxConcurrentRequests = 50 * Environment.ProcessorCount;

var server = new HttpServer("http://localhost:5000/", connectionString,  maxConcurrentRequests);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.StartAsync(cts.Token);