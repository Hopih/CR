using WordGuesser.Protocol;
using System.Net.Sockets;
using System.Text;

namespace WordGuesserServer;

[Command(Command.Guess)]
public class GuessCommandHandler : ICommandHandler
{
        public async Task Invoke(
        Socket sender,
        GameSessionRegistry sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var (sessionId, attempt) = GuessCommandHelper.DecodeSessionWithData(Command.Guess, payload!);
        var sessionCheck = await sessions.CheckSessionById(sessionId, sender, ct);
        if (!sessionCheck.HasSession)
            return;

        var session = sessionCheck.Session!;
        if (!await session.CheckSessionHost(sender, ct) ||
            !await session.CheckPlayer(sender, ct))
        {
            return;
        }

        // Игровое состояние
        if (session.GuessedWord == null)
        {
            await sender.SendIncorrectSender(ct); // слово не задано
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
            await session.Player.Connector.SendPacketAsync(
                GuessCommandHelper.Win(word), ct);
            await session.Hosted.Connector.SendPacketAsync(
                GuessCommandHelper.Win(word), ct);
        }
        else if (session.TryCount <= 0)
        {
            await sessions.EndAsync(session, ct: ct);
            return;
        }
        else
        {
            string mask = sb.ToString();
            await session.Player.Connector.SendPacketAsync(
                GuessCommandHelper.Result(mask), ct);
            await session.Hosted.Connector.SendPacketAsync(
                GuessCommandHelper.Result(mask), ct);
        }

        await sender.SendOk(ct);
    }

}
