using System.Net;
using System.Net.Sockets;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var socketClient = new Socket(
        AddressFamily.InterNetwork, 
        SocketType.Stream, 
        ProtocolType.Tcp);

//socketClient.Bind(
//   new IPEndPoint(new IPAddress(new byte[]{127, 0, 0, 1}),5000));

//await socketClient.ConnectAsync("google.com", 80);

//var message = "GET / HTTP/1.1\r\nHost: google.com\r\nConnection: close\r\n\r\n";

await socketClient.ConnectAsync("localhost", 5000);

var message = "Hello server!";

var bytesSent = await socketClient.SendAsync(
    new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
    SocketFlags.None);

Console.WriteLine($"send{bytesSent}");

var buffer = new byte[256];
var result = 0;
var resultMessage = new StringBuilder();
do
{
    result = await socketClient.ReceiveAsync(buffer, SocketFlags.None);
    resultMessage.Append(Encoding.UTF8.GetString(buffer));
    if (result < buffer.Length)
    {
        break;
    }
    
    result = await socketClient.ReceiveAsync(
        new ArraySegment<byte>(buffer), SocketFlags.None);
} while (result > 0);

Console.WriteLine(resultMessage.ToString());

await socketClient.DisconnectAsync(true);

socketClient.Dispose();