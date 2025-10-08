using Npgsql;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Entities;

const string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=white_board";

var maxConcurrentRequests = 50 * Environment.ProcessorCount;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<ElementType>("board_element_type");
var dataSource = dataSourceBuilder.Build();

var server = new HttpServer("http://localhost:5000/", dataSource,  maxConcurrentRequests);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.StartAsync(cts.Token);