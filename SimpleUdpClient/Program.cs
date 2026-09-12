using System.Net;
using System.Net.Sockets;
using System.Text;

var socket = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Dgram,
    ProtocolType.Udp);

var serverEndPoint = new IPEndPoint(
    IPAddress.Loopback,
    5000);

Console.Write("Message: ");

var message = Console.ReadLine() ?? "";

var data = Encoding.UTF8.GetBytes(message);

await socket.SendToAsync(data, serverEndPoint);

var buffer = new byte[1024];

EndPoint remoteEndPoint =
    new IPEndPoint(IPAddress.Any, 0);

var receivedBytes = socket.ReceiveFrom(
    buffer,
    ref remoteEndPoint);

var received = await socket.ReceiveFromAsync(
    buffer, remoteEndPoint);
//received.RemoteEndPoint;

var response = Encoding.UTF8.GetString(
    buffer,
    0,
    receivedBytes);

Console.WriteLine($"Server response: {response}");

socket.Close();