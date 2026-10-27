using System.Net;
using System.Net.Sockets;
using System.Text;
using NumberGuessServer;

Console.OutputEncoding = Encoding.UTF8;
using var shutdown = new CancellationTokenSource();
using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, 5050));
serverSocket.Listen(20);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
Console.WriteLine("Угадай число: TCP 127.0.0.1:5050 (Ctrl+C — выход)");

try
{
    while (!shutdown.IsCancellationRequested)
    {
        var clientSocket = await serverSocket.AcceptAsync(shutdown.Token);
        _ = HandleClientAsync(clientSocket, shutdown.Token);
    }
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }

static async Task HandleClientAsync(Socket socket, CancellationToken cancellationToken)
{
    Console.WriteLine($"Подключение: {socket.RemoteEndPoint}");
    using var clientSocket = socket;
    var buffer = new byte[512];
    var messageBytes = new List<byte>();
    var game = new GameSession();

    try
    {
        while (true)
        {
            var received = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cancellationToken);
            if (received == 0) break; // Клиент закрыл соединение.

            // В одном Receive могут быть часть команды или сразу несколько команд.
            for (var i = 0; i < received; i++)
            {
                if (buffer[i] != (byte)'\n')
                {
                    messageBytes.Add(buffer[i]);
                    if (messageBytes.Count <= 1024) continue;
                }

                var tooLong = messageBytes.Count > 1024;
                // Декодируем только полное сообщение: UTF-8 символ тоже может прийти частями.
                var response = tooLong
                    ? "ERROR LINE_TOO_LONG"
                    : game.HandleCommand(Encoding.UTF8.GetString(messageBytes.ToArray()).TrimEnd('\r'));
                messageBytes.Clear();
                var responseBytes = Encoding.UTF8.GetBytes(response + "\n");

                // Send не обязан отправлять весь массив за один вызов.
                var sent = 0;
                while (sent < responseBytes.Length)
                {
                    var count = await socket.SendAsync(responseBytes.AsMemory(sent), SocketFlags.None, cancellationToken);
                    if (count == 0) return;
                    sent += count;
                }

                if (response == "BYE" || tooLong) return;
            }
        }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    catch (SocketException ex) { Console.WriteLine($"Ошибка соединения: {ex.Message}"); }
}
