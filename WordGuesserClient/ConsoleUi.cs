using System.Text;

namespace WordGuesserClient;

public sealed class ConsoleUi : IClientUi
{
    private readonly object _sync = new();
    private readonly StringBuilder _line = new();
    private bool _reading;
    private int _drawn;
    private bool Interactive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    public void WriteLine(string message)
    {
        lock (_sync)
        {
            if (Interactive && _reading) ClearPrompt();
            Console.WriteLine(message);
            if (Interactive && _reading) DrawPrompt();
        }
    }

    public async ValueTask<string?> ReadLineAsync(CancellationToken ct)
    {
        if (Console.IsInputRedirected)
        {
            // Console.ReadLine cannot be cancelled on all platforms. The worker is a background
            // thread; WaitAsync lets the application exit immediately on server disconnect.
            return await Task.Run(Console.ReadLine, CancellationToken.None).WaitAsync(ct);
        }
        lock (_sync) { _reading = true; if (Interactive) DrawPrompt(); }
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                lock (_sync)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key is ConsoleKey.D or ConsoleKey.Z)
                            return null;
                        if (key.Key == ConsoleKey.Enter)
                        {
                            Console.WriteLine();
                            var result = _line.ToString();
                            _line.Clear();
                            _reading = false;
                            _drawn = 0;
                            return result;
                        }
                        if (key.Key == ConsoleKey.Backspace && _line.Length > 0) _line.Length--;
                        else if (key.Key == ConsoleKey.Escape) _line.Clear();
                        else if (!char.IsControl(key.KeyChar)) _line.Append(key.KeyChar);
                        if (Interactive) { ClearPrompt(); DrawPrompt(); }
                    }
                }
                await Task.Delay(25, ct);
            }
        }
        finally { lock (_sync) { _reading = false; _drawn = 0; } }
    }

    private void ClearPrompt() => Console.Write("\r" + new string(' ', _drawn) + "\r");

    private void DrawPrompt()
    {
        var width = Math.Max(1, Console.WindowWidth - 3);
        var text = _line.ToString();
        if (text.Length > width) text = text[^width..];
        Console.Write("> " + text);
        _drawn = text.Length + 2;
    }
}
