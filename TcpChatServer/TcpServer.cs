using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatServer;

public class TcpServer
{
    private readonly Socket _socket;
    private readonly Dictionary<Socket,string> _clients=new();

    public TcpServer(IPAddress address, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(address, port));
    }

    public async Task Start()
    {
        try
        {
            _socket.Listen();
            while (_socket.Connected)
            {
                var connectionSocket = await _socket.AcceptAsync();
                _ = Task.Run(
                    async () =>
                        await ProcessSocketConnect(connectionSocket));
            }
        }
        catch
        {
            _socket.Close();
        }
    }


    private async Task ProcessSocketConnect(Socket socket)
    {
        var buffer=new byte[256];
        var response = await socket.ReceiveAsync(buffer,SocketFlags.None);
        if (IsResponseValid(buffer))
        {
            if (IsHello(buffer))
            {
                var content=Encoding.UTF8.GetString(buffer.Skip(10).SkipLast(1).ToArray());
                _clients.Add(socket,content);
                var broadcastMessage = $"К нам присоединился {content}";
                Console.WriteLine(broadcastMessage);
                await socket.SendAsync(Encoding.UTF8.GetBytes(broadcastMessage), SocketFlags.None);
            }
        }
        else
        {
            Console.WriteLine("К нам пытался ворваться клиент");
        }
    }

    private bool IsResponseValid(byte[] buffer)=> HasStart(buffer) 
                                                  && HasEnd(buffer) 
                                                  && IsCorrectProtocol(buffer)
                                                  && IsFullOrPartial(buffer);

    private bool HasStart(byte[] buffer)=> buffer[..3].Equals(Package11207Helper.BasePackage[..3]);
    
    private bool HasEnd(byte[] buffer)=> buffer[^1].Equals(Package11207Helper.LastByte);
    
    private bool IsCorrectProtocol(byte[] buffer)=> buffer[3..8].Equals(Package11207Helper.BasePackage[3..8]);
    private bool IsFullOrPartial(byte[] buffer) => (buffer[9] == (byte)FullnessPackage.Partial)
                                                   || (buffer[9] == (byte)FullnessPackage.Full);

    private bool IsHello(byte[] buffer) => (buffer[8] == (byte)SupportCommand.Hello) 
                                           && (buffer[9] == (byte)FullnessPackage.Full);
    private bool IsSay(byte[] buffer) => buffer[8] == (byte)SupportCommand.Say;
    private bool IsBye(byte[] buffer) => buffer[8] == (byte)SupportCommand.Bye;
}