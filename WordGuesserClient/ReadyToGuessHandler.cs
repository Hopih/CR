using WordGuesser.Protocol;

namespace WordGuesserClient;

[ClientCommand(Command.ReadyToGuess)]
public class ReadyToGuessHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.Mask = GuessCommandHelper.Decode<string>(Command.ReadyToGuess, payload);
        client.AttemptsLeft = 5;
        client.State = ClientState.Guessing;
        client.Write($"Слово загадано: {client.Mask}. Попыток: 5.");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.GuessResult)]
public class GuessResultHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.Mask = GuessCommandHelper.Decode<string>(Command.GuessResult, payload);
        client.Pending = null;
        client.Write($"Маска: {client.Mask}");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.Win)]
public class WinHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.Write($"Слово отгадано: {GuessCommandHelper.Decode<string>(Command.Win, payload)}");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.End)]
public class EndHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        client.State = ClientState.Finished;
        client.SessionId = null;
        client.Role = null;
        client.Pending = null;
        client.Write("Сессия завершена.");
        return Task.CompletedTask;
    }
}
