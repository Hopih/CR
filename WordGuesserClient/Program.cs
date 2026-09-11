using System.Text;

namespace WordGuesserClient;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length > 2 || (args.Length > 0 && string.IsNullOrWhiteSpace(args[0])) ||
            (args.Length > 1 && (!int.TryParse(args[1], out var p) || p is < 1 or > 65535)))
        {
            Console.Error.WriteLine("Использование: WordGuesserClient [адрес сервера] [порт 1–65535]. По умолчанию: 127.0.0.1 5000.");
            return 1;
        }
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        var port = args.Length > 1 ? int.Parse(args[1]) : 5000;
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            var client = new GameClient(host, port);
            await client.Start(cancellation.Token);
            return client.HasError ? 1 : 0;
        }
        finally { Console.CancelKeyPress -= onCancel; }
    }
}
