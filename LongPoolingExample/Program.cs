using LongPoolingExample;

var server = new ChatServer();
await server.StartAsync("http://localhost:8080/");