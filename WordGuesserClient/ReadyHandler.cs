using WordGuesserServer;

namespace WordGuesserClient;

[ClientCommand(Command.Ready)]
public class ReadyHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.Role = GuessCommandHelper.Decode<PlayerRole>(Command.Ready, payload);
        ConsoleUi.Success($"Your role: {client.Role}");
        return Task.CompletedTask;
    }
}