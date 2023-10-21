using System.Net;
using System.Net.Sockets;
using System.Text;

var socketServer = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp);

socketServer.Bind(
   new IPEndPoint(new IPAddress(new byte[]{127, 0, 0, 1}),5000));
socketServer.Listen();


var connectionSocket = await socketServer.AcceptAsync();

var buffer = new byte[256];
var result = 0;
var resultMessage = new StringBuilder();
do
{
    result = await connectionSocket.ReceiveAsync(buffer, SocketFlags.None);
    resultMessage.Append(Encoding.UTF8.GetString(buffer));
    if (result < buffer.Length)
    {
        break;
    }

    result = await connectionSocket.ReceiveAsync(
        new ArraySegment<byte>(buffer), SocketFlags.None);
} while (result > 0);


Console.WriteLine(resultMessage.ToString());

var bytesSent = await connectionSocket.SendAsync(
    new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello client!")),
    SocketFlags.None);

Console.WriteLine($"send{bytesSent}");

await socketServer.DisconnectAsync(true);

socketServer.Dispose();



