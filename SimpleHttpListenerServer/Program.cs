using Microsoft.Extensions.Configuration;
using Npgsql;
using SimpleHttpListenerServer;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Core.MiniOrm;
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
services.AddTransient<IBoardsRepository, OrmBoardsRepository>();
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
services.AddSingleton<MiniOrmContext>();

services.RegisterValidators();
services.AddControllers();


var maxConcurrentRequests = configuration.GetValue<int>("CountOfProcess") * Environment.ProcessorCount;

JwtWorker.SecretKey = configuration["Jwt:SecretKey"]!;
JwtWorker.Audience = configuration["Jwt:Audience"]!;
JwtWorker.Issuer = configuration["Jwt:Issuer"]!;
JwtWorker.TimeToLeave = configuration.GetValue<int>("Jwt:TimeToLeave");


services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration["Database:ConnectionString"]!);
    dataSourceBuilder.MapEnum<ElementType>("board_element_type");
    return dataSourceBuilder.Build();
});


var sp = services.BuildServiceProvider();
sp.UseControllers();

var pipeline = new MiddlewarePipeline()
    .Use<ErrorHandlingMiddleware>()
    .Use<AuthMiddleware>()
    .Use<ValidationMiddleware>()
    .Use<RoutingMiddleware>()
    .Use<ExecutionMiddleware>()
    .Use<StaticFileMiddleware>();
    
    
var server = new HttpServer(configuration["Host"]!, pipeline, sp, maxConcurrentRequests);

var cts = new CancellationTokenSource();

await server.StartAsync(cts);