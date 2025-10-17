using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Helpers;
using SimpleHttpListenerServer.Pipeline;
using ServiceCollection = SimpleHttpListenerServer.Core.ServiceCollection;

const string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=white_board";


var services = new ServiceCollection();

// добавляем middleware как Singleton
services.AddSingleton<BoardsRepository>();
services.AddSingleton<ElementsRepository>();
services.AddSingleton<UsersRepository>();

// Middleware, Router и т.д.
services.AddSingleton<AuthMiddleware>();
services.AddSingleton<RouteTable>();
services.AddSingleton<ErrorHandlingMiddleware>();
services.AddSingleton<ValidationMiddleware>();
services.AddSingleton(new StaticFileMiddleware(Path.Combine(AppContext.BaseDirectory, "public")));

services.RegisterValidators();


var maxConcurrentRequests = 50 * Environment.ProcessorCount;


services.AddSingleton(sp =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.MapEnum<ElementType>("board_element_type");
    return dataSourceBuilder.Build();
});


var sp = services.BuildServiceProvider();
sp.AddControllers();

var pipeline = new MiddlewarePipeline()
    .Use<ErrorHandlingMiddleware>(sp)
    .Use<AuthMiddleware>(sp)
    .Use<ValidationMiddleware>(sp)
    .Use(async (ctx, next) =>
    {
        var routeTable = sp.GetService<RouteTable>();
        var router = new RoutingMiddleware(routeTable!, sp);
        await router.InvokeAsync(ctx, next);
    })
    .Use<StaticFileMiddleware>(sp);
    
    
var server = new HttpServer("http://localhost:5000/", pipeline, sp, maxConcurrentRequests);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.StartAsync(cts.Token);