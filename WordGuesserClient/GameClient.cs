using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserClient;

public class GameClient
{
    private readonly Socket _socket;
    private readonly GuessClientHelper _helper;

    public Guid? SessionId { get; set; }
    public PlayerRole? Role { get; set; }
    public bool Running { get; set; } = true;

    public GameClient(string host, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(host, port);
        _helper = new GuessClientHelper(_socket);
    }

    public async Task Start()
    {
        ConsoleUi.Info("Connected to server.");

        // start listen loop
        _ = Task.Run(ListenLoop);

        while (Running)
        {
            try
            {
                await HandleInput();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                ConsoleUi.Error(ex.Message);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                ConsoleUi.Error($"Connection closed: {ex.Message}");
                Running = false;
            }
        }
        _socket.Dispose();
    }

    private async Task HandleInput()
    {
        switch (Role)
        {
            case PlayerRole.Guesser:
                ConsoleUi.Info("Commands: guess | exit");
                break;
            case null:
                ConsoleUi.Info("Commands: create | join <session>");
                break;
            case PlayerRole.Hosted:
                ConsoleUi.Info("Commands: word | exit");
                break;
            default:
                ConsoleUi.Info("Commands: guess | exit");
                break;
        }

        var line = ConsoleUi.Input("> ");
        var parts = line.Split(' ', 2);

        //TODO: Factory
        switch (parts[0].ToLower())
        {
            case "exit":
                Running = false;
                return;

            case "create":
                await _helper.SendCreate();
                break;

            case "join":
                if (parts.Length < 2) { ConsoleUi.Error("Usage: join <session>"); break; }
                if (Guid.TryParse(parts[1], out var sid))
                    await _helper.SendJoin(sid);
                else
                    ConsoleUi.Error("Invalid guid.");
                break;

            case "word":
                if (Role != PlayerRole.Hosted) { ConsoleUi.Error("You are not host"); break; }
                if (SessionId == null) { ConsoleUi.Error("No session"); break; }

                var w = ConsoleUi.Input("Enter word: ").ToUpperInvariant();
                await _helper.SendWord(SessionId.Value, w);
                break;

            case "guess":
                if (Role != PlayerRole.Guesser) { ConsoleUi.Error("Not guesser"); break; }
                if (SessionId == null) { ConsoleUi.Error("No session"); break; }

                var g = ConsoleUi.Input("Enter guess: ").ToUpperInvariant();
                await _helper.SendGuess(SessionId.Value, g);
                break;

            default:
                ConsoleUi.Error("Unknown command");
                break;
        }
    }

    private async Task ListenLoop()
    {
        try
        {
            while (Running)
            {
                var parsed = await _socket.ReceivePacketAsync();
                if (parsed == null)
                    break;

                GuessCommandHelper.Decode(parsed.Value.Command, parsed.Value.Payload);
                await HandleServer(parsed.Value.Command, parsed.Value.Payload);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or SocketException or ObjectDisposedException or FormatException)
        {
            if (Running)
                ConsoleUi.Error($"Connection closed: {ex.Message}");
        }
        finally
        {
            Running = false;
            _socket.Dispose();
        }
    }

    private async Task HandleServer(Command cmd, byte[] payload)
    {
        //TODO: ClientHandlers
        switch (cmd)
        {
            case Command.AttemptResult:
                var attempt = GuessCommandHelper.Decode<AttemptResult>(cmd, payload);
                ConsoleUi.Info($"Mask: {attempt.Mask}. Attempts left: {attempt.AttemptsLeft}");
                break;
            case Command.RoundResult:
                var round = GuessCommandHelper.Decode<RoundResult>(cmd, payload);
                ConsoleUi.Info($"Round {(round.Won ? "won" : "lost")}. Word: {round.Word}. Score (you : opponent): {round.YourScore} : {round.OpponentScore}");
                break;
            case Command.MatchResult:
                var match = GuessCommandHelper.Decode<MatchResult>(cmd, payload);
                ConsoleUi.Info($"Match {(match.Won ? "won" : "lost")}! Score: {match.YourScore} : {match.OpponentScore}");
                break;
            case Command.CommandResponse:
                var resp = GuessCommandHelper.Decode<CommandResponse>(cmd, payload);
                ConsoleUi.Info($"[Server Response] {resp}");
                break;

            case Command.SessionId:
                SessionId = GuessCommandHelper.Decode<Guid>(cmd, payload);
                ConsoleUi.Success($"Session created: {SessionId}");
                break;

            case Command.Ready:
                Role = GuessCommandHelper.Decode<PlayerRole>(cmd, payload);
                ConsoleUi.Success($"Your role: {Role}");
                break;

            case Command.ReadyToGuess:
                var mask = GuessCommandHelper.Decode<string>(cmd, payload);
                ConsoleUi.Success($"Mask: {mask}");
                break;

            case Command.GuessResult:
                var res = GuessCommandHelper.Decode<string>(cmd, payload);
                ConsoleUi.Info($"Result: {res}");
                break;

            case Command.Win:
                var word = GuessCommandHelper.Decode<string>(cmd, payload);
                ConsoleUi.Success($"🎉 You win! Word: {word}");
                break;

            case Command.Swap:
                ConsoleUi.Info("Swapping roles...");
                break;

            case Command.End:
                ConsoleUi.Success("🏁 Game finished");
                Running = false;
                break;

            default:
                ConsoleUi.Info($"Unhandled command: {cmd}");
                break;
        }
    }
}
