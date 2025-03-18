using System.Net.Sockets;
using System.Text;
using CodeJam;
using TcpChatServer;
using static TcpChatServer.Package11207Helper;

using var clientSocket = new Socket(AddressFamily.InterNetwork,
    SocketType.Stream, ProtocolType.Tcp);
try
{
    await clientSocket.ConnectAsync("localhost", 5000);

    Console.WriteLine("Кто ты, воин?");
    var name = Console.ReadLine();

    var content = Encoding.UTF8.GetBytes(name!);

    var helloPackage = CreatePackage(content, SupportCommand.Hello, FullnessPackage.Full, QueryType.Request);

    await clientSocket.SendAsync(new ArraySegment<byte>(helloPackage), SocketFlags.None);

    do
    {
        var response = await GetResponse(clientSocket);

        Console.WriteLine(response);

        Console.WriteLine("Введите команду");
        var command = Console.ReadLine();
        if (command != "q")
        {
            var message = Console.ReadLine();
            var packages = GetPackagesByMessage(
                Encoding.UTF8.GetBytes(message!), SupportCommand.Say, QueryType.Request);

            foreach (var package in packages)
            {
                await clientSocket.SendAsync(package, SocketFlags.None);
            }
        }
        else
        {
            var byePackage = new PackageBuilder(0)
                .WithCommand(SupportCommand.Bye)
                .WithFullness(FullnessPackage.Full)
                .WithQueryType(QueryType.Request)
                .Build();

            await clientSocket.SendAsync(byePackage, SocketFlags.None);
            Console.WriteLine("bye bye!");

            break;
        }

    } while (clientSocket.Connected);
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToDiagnosticString());
}
finally
{
    await clientSocket.DisconnectAsync(false);
}

async Task<string> GetResponse(Socket socket)
{
    var buffer = new byte[MaxPacketSize];
    var responseContent = new List<byte>();
    do
    {
        var contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None);
        
        if (!IsResponse(buffer[Query]) || !IsSay(buffer[Command]))
        {
            return "Получили неизвестный ответ от сервера!";
        }

        responseContent.AddRange(GetContent(buffer, contentLength));

    } while (!IsFull(buffer[Fullness]));

    return Encoding.UTF8.GetString(responseContent.ToArray());
}