using WordGuesser.Protocol;

namespace WordGuesserClient;

[ClientCommand(Command.SessionId)]
public class SessionIdHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.SessionId = GuessCommandHelper.Decode<Guid>(Command.SessionId, payload);
        ConsoleUi.Success($"Session created: {client.SessionId}");
        return Task.CompletedTask;
    }
}