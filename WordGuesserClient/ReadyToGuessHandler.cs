using WordGuesserServer;

namespace WordGuesserClient;

[ClientCommand(Command.ReadyToGuess)]
public class ReadyToGuessHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var mask = GuessCommandHelper.Decode<string>(Command.ReadyToGuess, payload);
        ConsoleUi.Info($"Mask: {mask}");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.GuessResult)]
public class GuessResultHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var result = GuessCommandHelper.Decode<string>(Command.GuessResult, payload);
        ConsoleUi.Info($"Guess result: {result}");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.Win)]
public class WinHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var word = GuessCommandHelper.Decode<string>(Command.Win, payload);
        ConsoleUi.Success($"🎉 You win! Word: {word}");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.End)]
public class EndHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        ConsoleUi.Success("🏁 Game finished!");
        client.Running = false;
        return Task.CompletedTask;
    }
}