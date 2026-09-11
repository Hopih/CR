using WordGuesser.Protocol;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Swap)]
public class SwapCommandHandler : ICommandHandler
{
    public async Task Invoke(
        Socket sender,
        GameSessionRegistry sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var session = sessions.FindByPlayer(sender);

        if (session == null)
        {
            await sender.SendSessionNotFound(ct);
            return;
        }

        if (!await session.CheckPlayer(sender, ct))
        {
            return;
        }

        // Проверяем, не завершён ли матч
        if (session.Hosted.WinCount >= 3 || session.Player!.WinCount >= 3)
        {
            await sessions.EndAsync(session, ct: ct);
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

        await session.Hosted.Connector.SendOk(ct);
        await session.Player.Connector.SendOk(ct);
    }
}
