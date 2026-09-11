using System.Net;
using System.Net.Sockets;

namespace WordGuesserServer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length > 2 || (args.Length > 0 && (!int.TryParse(args[0], out var requestedPort) || requestedPort is < 1 or > 65535)) ||
            (args.Length > 1 && !IPAddress.TryParse(args[1], out _)))
        {
            await Console.Error.WriteLineAsync("Usage: WordGuesserServer [port: 1-65535] [bind IP]. Defaults: 5000, 127.0.0.1.");
            return 1;
        }
        var port = args.Length > 0 ? int.Parse(args[0]) : 5000;
        var address = args.Length > 1 ? IPAddress.Parse(args[1]) : IPAddress.Loopback;
        var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            await using var server = new WgServer(new IPEndPoint(address, port));
            Console.WriteLine($"WordGuesser server listening on {server.EndPoint}. Press Ctrl+C to stop.");
            await server.StartServer(cancellation.Token);
            Console.WriteLine("Server stopped.");
            return 0;
        }
        catch (SocketException ex)
        {
            await Console.Error.WriteLineAsync($"Cannot start or run server: {ex.Message}");
            return 1;
        }
        finally { Console.CancelKeyPress -= onCancel; }
    }
}
