using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpChatServer;
using static TcpChatServer.Package11207Helper;

var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

var client = new TcpChatClient.TcpClient(clientSocket);

var ipAddress = new IPAddress(Encoding.UTF8.GetBytes("127.0.0.1"));

await client.ConnectAsync(ipAddress,5000);

Console.WriteLine("Кто ты, воин?");
var name = Console.ReadLine();

var content = Encoding.UTF8.GetBytes(name!);

var helloPackage = CreatePackage(content, SupportCommand.Hello, FullnessPackage.Full, QueryType.Request);

await clientSocket.SendAsync(new ArraySegment<byte>(helloPackage), SocketFlags.None);



do
{
    var response = GetResponse(clientSocket);
    Console.WriteLine("Введите команду");
    var command = Console.ReadLine();
    if (command != "q")
    {
        var message = Console.ReadLine();
        
    }

} while (clientSocket.Connected);


async Task<string> GetResponse(Socket socket)
{
    var buffer = new byte[MaxPacketSize];
    var responseContent = new List<byte>();
    do
    {
        var contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None);
        
        if (!IsResponse(buffer[Query]) || !IsSay(Command))
        {
            return "Получили неизвестный ответ от сервера!";
        }

        responseContent.AddRange(GetContent(buffer, contentLength));

    } while (!IsFull(buffer[Fullness]));

    return Encoding.UTF8.GetString(responseContent.ToArray());
}


async Task<List<byte[]>> GetRequest(string message, SupportCommand supportCommand)
{
    return new List<byte[]>();
}