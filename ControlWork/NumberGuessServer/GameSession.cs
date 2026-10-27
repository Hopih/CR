namespace NumberGuessServer;

// Один объект на подключение. У разных клиентов независимые игры.
public sealed class GameSession
{
    private int secret;
    private bool isPlaying;

    public string HandleCommand(string message)
    {
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ERROR UNKNOWN_COMMAND";

        switch (parts[0].ToUpperInvariant())
        {
            case "START":
                if (parts.Length != 1) return "ERROR INVALID_ARGUMENTS";
                secret = Random.Shared.Next(1, 101);
                isPlaying = true;
                return "OK";

            case "GUESS":
                if (parts.Length != 2 || !int.TryParse(parts[1], out var guess) || guess < 1 || guess > 100)
                    return "ERROR INVALID_NUMBER";
                if (!isPlaying) return "ERROR NOT_STARTED";
                if (guess < secret) return "GREATER";
                if (guess > secret) return "LESS";
                isPlaying = false;
                return "WIN";

            case "QUIT":
                return parts.Length == 1 ? "BYE" : "ERROR INVALID_ARGUMENTS";

            default:
                return "ERROR UNKNOWN_COMMAND";
        }
    }
}
