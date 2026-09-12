using System.Net;
using System.Net.Sockets;
using System.Text;

var socket = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Dgram,
    ProtocolType.Udp);

socket.Bind(new IPEndPoint(IPAddress.Any, 5000));

Console.WriteLine("UDP server started on port 5000");

var buffer = new byte[1024];

while (true)
{
    EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

    var receivedBytes = socket.ReceiveFrom(
        buffer,
        ref clientEndPoint);

    var message = Encoding.UTF8.GetString(
        buffer,
        0,
        receivedBytes);

    Console.WriteLine($"Received from {clientEndPoint}: {message}");

    socket.SendTo(
        buffer,
        0,
        receivedBytes,
        SocketFlags.None,
        clientEndPoint);
}