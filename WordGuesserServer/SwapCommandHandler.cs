using WordGuesser.Protocol;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Swap)]
public class SwapCommandHandler : ICommandHandler
{
    public async Task Invoke(
        Socket sender,
        ConcurrentDictionary<Guid, GameSession> sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var session = sessions.Values.FirstOrDefault(s =>
            s.Hosted.Connector.Equals(sender) || (s.Player?.Connector.Equals(sender) ?? false));

        if (session == null)
        {
            await sender.SendSessionNotFound();
            return;
        }

        if (!await session.CheckPlayer(sender))
        {
            return;
        }

        // Проверяем, не завершён ли матч
        if (session.Hosted.WinCount >= 3 || session.Player!.WinCount >= 3)
        {
            await session.Hosted.Connector.SendPacketAsync(GuessCommandHelper.End(), ct);
            await session.Player!.Connector.SendPacketAsync(GuessCommandHelper.End(), ct);
            sessions.TryRemove(session.Id, out _);
            return;
        }

        // Меняем роли
        (session.Hosted, session.Player) = (session.Player, session.Hosted);
        session.Hosted.Role = PlayerRole.Hosted;
        session.Player.Role = PlayerRole.Guesser;
        session.GuessedWord = null;
        session.TryCount = 5;

        // Уведомляем игроков
        await session.Hosted.Connector.SendPacketAsync(
            GuessCommandHelper.Ready(PlayerRole.Hosted), ct);
        await session.Player.Connector.SendPacketAsync(
            GuessCommandHelper.Ready(PlayerRole.Guesser), ct);

        await session.Hosted.Connector.SendOk();
        await session.Player.Connector.SendOk();
    }
}