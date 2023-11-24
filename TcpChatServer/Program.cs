using System.Net;
using TcpChatServer;

var server = new TcpServer(new IPAddress(new byte[] { 127, 0, 0, 1 }), 5000);

await server.StartAsync();

Console.WriteLine("Server was stoped!");





