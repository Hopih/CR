namespace WordGuesserClient;

public enum ClientState
{
    Connecting, Lobby, WaitingForPlayer, WaitingForWord, SettingWord,
    Guessing, Watching, WaitingForRound, Finished
}

public interface IClientUi
{
    ValueTask<string?> ReadLineAsync(CancellationToken ct);
    void WriteLine(string message);
}
