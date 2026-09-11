using WordGuesser.Protocol;
using System.Net.Sockets;
using System.Text;

namespace WordGuesserServer;

[Command(Command.Word)]
public class WordCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, GameSessionRegistry gameSessions,
        byte[]? payload = null, CancellationToken ct = default)
    {
        var (sessionId, word) = GuessCommandHelper.DecodeSessionWithData(Command.Word, payload!);
        var sessionCheck = await gameSessions.CheckSessionById(sessionId, sender, ct);
        
        if (!sessionCheck.HasSession)
        {
            return;
        }
        
        var gameSession = sessionCheck.Session!;

        if (!await gameSession.CheckSessionHost(sender, ct) ||
            !await gameSession.CheckPlayer(sender, ct))
        {
            return;
        }
        
        gameSession.GuessedWord = word;

        var sb = new StringBuilder();
        sb.Append('*', gameSession.GuessedWord.Length);
        
        await gameSession.Hosted.Connector.SendOk(ct);
        
        await gameSession.Player!.Connector.SendReadyToGuess(sb.ToString(), ct);
    }
}
