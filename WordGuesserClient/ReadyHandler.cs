using WordGuesser.Protocol;

namespace WordGuesserClient;

[ClientCommand(Command.Ready)]
public class ReadyHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.Role = GuessCommandHelper.Decode<PlayerRole>(Command.Ready, payload);
        client.State = client.Role == PlayerRole.Hosted ? ClientState.SettingWord : ClientState.WaitingForWord;
        client.Pending = null;
        client.Mask = "";
        client.AttemptsLeft = 5;
        client.Write(client.Role == PlayerRole.Hosted ? "Новый раунд: вы загадываете." : "Новый раунд: вы угадываете.");
        return Task.CompletedTask;
    }
}
