using System.Net.Sockets;

namespace NumberGuessServer;

// Один объект на подключение. У разных клиентов независимые игры.
public sealed class GameSession
{
    private int secret;
    private bool isPlaying;
    private bool isFirst = false;
    private (int,int) range = (1,100);
    private bool flag = true;
    public static Dictionary<Socket, int> clients = new Dictionary<Socket, int>();
    public static Dictionary<Socket, int> copyClients = new Dictionary<Socket, int>();
    private static int best = 0;
    public string HandleCommand(string message, Socket socket)
    {
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ERROR UNKNOWN_COMMAND";

        switch (parts[0].ToUpperInvariant())
        {
            case "START":
                if (parts.Length != 1) return "ERROR INVALID_ARGUMENTS";
                range = (1, 100);
                secret = Random.Shared.Next(1, 101);
                clients[socket] = 0;
                copyClients[socket] = 0;
                isPlaying = true;
                isFirst = false;
                return "OK";

            case "GUESS":
                if (parts.Length != 2 || !int.TryParse(parts[1], out var guess) || guess < 1 || guess > 100)
                    return "ERROR INVALID_ARGUMENTS";
                if (!isPlaying) return "ERROR NOT_STARTED";
                if (guess < secret)
                {
                    range = (Math.Max(guess + 1, range.Item1), range.Item2);
                    copyClients[socket] += 1;
                    return "GREATER";
                }

                if (guess > secret)
                {
                    range = (range.Item1, Math.Min(guess - 1, range.Item2));
                    copyClients[socket] += 1;
                    return "LESS";
                }

                isPlaying = false;
                isFirst = true;
                if (flag)
                {
                    clients[socket] = Math.Max(copyClients[socket], clients[socket])+1;
                    flag = false;
                }
                else
                {
                    clients[socket] = Math.Min(copyClients[socket], clients[socket])+1;
                }

                var result = copyClients[socket]+1;
                copyClients[socket] = 0;
                if (best == 0)
                {
                    best = result;
                }
                else
                {
                    best = Math.Min(best, result);
                }
                return $"WIN {result}";

            case "STATUS":
                if (parts.Length != 1) return "ERROR INVALID_ARGUMENTS";
                if (isFirst) return "STATUS WON";
                if (!isPlaying) return "STATUS NOT_STARTED";
                if (isPlaying) return $"STATUS PLAYING {range.Item1}-{range.Item2}";
                return "ERROR UNKNOWN_COMMAND";
            
            case "RECORD":
                if (best == 0) return "RECORD NONE";
                return $"RECORD {best}";
            
            case "QUIT":
                return parts.Length == 1 ? "BYE" : "ERROR INVALID_ARGUMENTS";

            default:
                return "ERROR INVALID_ARGUMENTS";
        }
    }
}
