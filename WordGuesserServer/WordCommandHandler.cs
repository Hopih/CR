using WordGuesser.Protocol;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace WordGuesserServer;

[Command(Command.Word)]
public class WordCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null, CancellationToken ct = default)
    {
        var (sessionId, word) = GuessCommandHelper.DecodeSessionWithData(Command.Word, payload!);
        var sessionCheck = await gameSessions.CheckSessionById(sessionId, sender);
        
        if (!sessionCheck.HasSession)
        {
            return;
        }
        
        var gameSession = sessionCheck.Session!;

        if (!await gameSession.CheckSessionHost(sender) ||
            !await gameSession.CheckPlayer(sender))
        {
            return;
        }
        
        gameSession.GuessedWord = word;

        var sb = new StringBuilder();
        sb.Append('*', gameSession.GuessedWord.Length);
        
        await gameSession.Hosted.Connector.SendOk();
        
        await gameSession.Player!.Connector.SendReadyToGuess(sb.ToString());
    }
}
