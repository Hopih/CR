using WordGuesser.Protocol;

namespace WordGuesserClient;

[ClientCommand(Command.SessionId)]
public class SessionIdHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.SessionId = GuessCommandHelper.Decode<Guid>(Command.SessionId, payload);
        client.State = client.Pending == Command.Join ? ClientState.WaitingForWord : ClientState.WaitingForPlayer;
        client.Pending = null;
        client.YourScore = 0;
        client.OpponentScore = 0;
        client.Mask = "";
        client.AttemptsLeft = 0;
        client.Write($"Сессия подтверждена: {client.SessionId}");
        return Task.CompletedTask;
    }
}
