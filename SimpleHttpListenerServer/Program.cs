using Microsoft.Extensions.Configuration;
using Npgsql;
using SimpleHttpListenerServer;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Helpers;
using SimpleHttpListenerServer.Pipeline;
using ServiceCollection = SimpleHttpListenerServer.Core.ServiceCollection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .Build();

var services = new ServiceCollection();

// добавляем middleware как Singleton
services.AddTransient<BoardsRepository>();
services.AddTransient<ElementsRepository>();
services.AddTransient<UsersRepository>();
services.AddSingleton<QuotesApiClient>();

// Middleware, Router и т.д.
services.AddSingleton<AuthMiddleware>();
services.AddSingleton<RouteTable>();
services.AddSingleton<ErrorHandlingMiddleware>();
services.AddSingleton<ValidationMiddleware>();
services.AddSingleton<RoutingMiddleware>();
services.AddSingleton(new StaticFileMiddleware(Path.Combine(AppContext.BaseDirectory, configuration["StaticFiles"]!)));
services.AddSingleton<ExecutionMiddleware>();

services.RegisterValidators();
services.AddControllers();


var maxConcurrentRequests = configuration.GetValue<int>("CountOfProcess") * Environment.ProcessorCount;

JwtWorker.SecretKey = configuration["Jwt:SecretKey"]!;
JwtWorker.Audience = configuration["Jwt:Audience"]!;
JwtWorker.Issuer = configuration["Jwt:Issuer"]!;
JwtWorker.TimeToLeave = configuration.GetValue<int>("Jwt:TimeToLeave")!;


services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration["Database:ConnectionString"]!);
    dataSourceBuilder.MapEnum<ElementType>("board_element_type");
    return dataSourceBuilder.Build();
});


var sp = services.BuildServiceProvider();
sp.UseControllers();

var pipeline = new MiddlewarePipeline()
    .Use<ErrorHandlingMiddleware>(sp)
    .Use<AuthMiddleware>(sp)
    .Use<ValidationMiddleware>(sp)
    .Use<RoutingMiddleware>(sp)
    .Use<ExecutionMiddleware>(sp)
    .Use<StaticFileMiddleware>(sp);
    
    
var server = new HttpServer(configuration["Host"]!, pipeline, sp, maxConcurrentRequests);

var cts = new CancellationTokenSource();

await server.StartAsync(cts);