using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserServer;

[Command(Command.Word)]
public class WordCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, GameSessionRegistry sessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var (id, word) = GuessCommandHelper.DecodeSessionWithData(Command.Word, payload!);
        var session = sessions.Find(id);
        if (session == null) { await sender.SendSessionNotFound(ct); return; }
        if (!ReferenceEquals(session.Hosted.Connector, sender)) { await sender.SendIncorrectSender(ct); return; }
        if (session.Player == null) { await sender.SendPlayerNotJoined(ct); return; }
        var result = GameRules.SetWord(session, word);
        await sender.SendResponseCommand(result, ct);
        if (result == CommandResponse.Ok)
            await session.Player.Connector.SendReadyToGuess(new string('*', word.Length), ct);
    }
}
