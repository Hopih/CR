using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace WordGuesserServer;

[Command(Command.Guess)]
public class GuessCommandHandler : ICommandHandler
{
        public async Task Invoke(
        Socket sender,
        ConcurrentDictionary<Guid, GameSession> sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var (sessionId, attempt) = DecodeSession(payload!);
        var sessionCheck = await sessions.CheckSessionById(sessionId, sender);
        if (!sessionCheck.HasSession)
            return;

        var session = sessionCheck.Session!;
        if (!await session.CheckSessionHost(sender) ||
            !await session.CheckPlayer(sender))
        {
            return;
        }

        // Игровое состояние
        if (session.GuessedWord == null)
        {
            await sender.SendIncorrectSender(); // слово не задано
            return;
        }

        string word = session.GuessedWord.ToUpperInvariant();
        string guess = attempt.ToUpperInvariant();

        var sb = new StringBuilder();
        int matched = 0;

        for (int i = 0; i < word.Length; i++)
        {
            if (i < guess.Length && word[i] == guess[i])
            {
                sb.Append(word[i]);
                matched++;
            }
            else
                sb.Append('*');
        }

        bool win = sb.ToString().Equals(word, StringComparison.OrdinalIgnoreCase);
        session.TryCount--;

        if (win)
        {
            session.Player.WinCount++;
            await session.Player.Connector.SendAsync(
                GuessCommandHelper.Win(word), SocketFlags.None);
            await session.Hosted.Connector.SendAsync(
                GuessCommandHelper.Win(word), SocketFlags.None);
        }
        else if (session.TryCount <= 0)
        {
            await session.Player.Connector.SendAsync(
                GuessCommandHelper.End(), SocketFlags.None);
            await session.Hosted.Connector.SendAsync(
                GuessCommandHelper.End(), SocketFlags.None);
        }
        else
        {
            string mask = sb.ToString();
            await session.Player.Connector.SendAsync(
                GuessCommandHelper.Result(mask), SocketFlags.None);
            await session.Hosted.Connector.SendAsync(
                GuessCommandHelper.Result(mask), SocketFlags.None);
        }

        await sender.SendOk();
    }

    private static (Guid sessionId, string attempt) DecodeSession(byte[] payload)
    {
        var decoded = GuessCommandHelper.Decode<string>(Command.Guess, payload)!;
        var parts = decoded.Split(':', 2);
        return (Guid.Parse(parts[0]), parts[1]);
    }
}