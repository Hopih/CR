namespace WordGuesserClient;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Server IP (default 127.0.0.1): ");
        var ip = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        Console.Write("Server Port (default 5000): ");
        var portStr = Console.ReadLine();
        if (!int.TryParse(portStr, out var port)) port = 5000;

        var client = new GameClient(ip, port);
        await client.Start();
    }
}