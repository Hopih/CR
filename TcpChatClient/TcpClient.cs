using System.Net;
using System.Net.Sockets;

namespace TcpChatClient;

public class TcpClient
{
    private readonly Socket _socket;
    

    public async Task ConnectAsync(IPAddress host, int port)
    {
        await _socket.ConnectAsync(host, port);
    }
}