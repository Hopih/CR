using WordGuesser.Protocol;

namespace WordGuesserClient;

[ClientCommand(Command.CommandResponse)]
public class ResponseHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var response = GuessCommandHelper.Decode<CommandResponse>(Command.CommandResponse, payload);
        if (response == CommandResponse.Ok)
        {
            if (client.State == ClientState.Connecting) client.State = ClientState.Lobby;
            else if (client.Pending == Command.Word)
            {
                client.State = ClientState.Watching;
                client.AttemptsLeft = 5;
                client.Write("Слово принято сервером.");
            }
        }
        else
        {
            client.Write(response switch
            {
                CommandResponse.SessionNotFound => "Сессия не найдена или уже завершена.",
                CommandResponse.SessionFull => "В этой сессии уже два игрока.",
                CommandResponse.AlreadyInSession => "Вы уже участвуете в сессии.",
                CommandResponse.PlayerNotJoined => "Второй игрок ещё не подключился.",
                CommandResponse.IncorrectSender => "Это действие недоступно для вашей роли.",
                CommandResponse.InvalidState => "Сейчас это действие недоступно. Дождитесь следующего события игры.",
                CommandResponse.InvalidWord => "В слове допустимы только русские и английские буквы.",
                CommandResponse.InvalidGuess => "Попытка должна состоять из букв и совпадать со словом по длине.",
                CommandResponse.PackageIncorrect => "Сервер отклонил данные команды.",
                _ => "Сервер не поддерживает эту команду."
            });
            if (response == CommandResponse.SessionNotFound)
            {
                client.State = ClientState.Lobby;
                client.SessionId = null;
                client.Role = null;
            }
        }
        client.Pending = null;
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.AttemptResult)]
public class AttemptResultHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var result = GuessCommandHelper.Decode<AttemptResult>(Command.AttemptResult, payload);
        client.Mask = result.Mask;
        client.AttemptsLeft = result.AttemptsLeft;
        client.Pending = null;
        client.State = result.AttemptsLeft == 0 || !result.Mask.Contains('*')
            ? ClientState.WaitingForRound
            : client.Role == PlayerRole.Guesser ? ClientState.Guessing : ClientState.Watching;
        client.Write($"Маска: {result.Mask}. Осталось попыток: {result.AttemptsLeft}.");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.RoundResult)]
public class RoundResultHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var result = GuessCommandHelper.Decode<RoundResult>(Command.RoundResult, payload);
        client.YourScore = result.YourScore;
        client.OpponentScore = result.OpponentScore;
        client.State = ClientState.WaitingForRound;
        client.Write($"Раунд {(result.Won ? "выигран" : "проигран")}. Слово: {result.Word}. Счёт (вы : соперник): {result.YourScore}:{result.OpponentScore}.");
        return Task.CompletedTask;
    }
}

[ClientCommand(Command.MatchResult)]
public class MatchResultHandler : IClientCommandHandler
{
    public Task Handle(GameClient client, byte[] payload)
    {
        var result = GuessCommandHelper.Decode<MatchResult>(Command.MatchResult, payload);
        client.YourScore = result.YourScore;
        client.OpponentScore = result.OpponentScore;
        client.State = ClientState.WaitingForRound;
        client.Write($"Матч {(result.Won ? "выигран" : "проигран")}! Итоговый счёт: {result.YourScore}:{result.OpponentScore}.");
        return Task.CompletedTask;
    }
}
