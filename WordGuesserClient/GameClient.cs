using System.Net.Sockets;
using WordGuesserServer;

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
            await HandleInput();
        }
    }

    private async Task HandleInput()
    {
        switch (Role)
        {
            case PlayerRole.Guesser:
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
        byte[] buf = new byte[256];

        while (Running)
        {
            int n = await _socket.ReceiveAsync(buf, SocketFlags.None);
            if (n <= 0) break;

            var span = buf.AsSpan(0, n);
            var parsed = GuessPackageParser.TryParse(span, out var code);

            if (parsed == null)
            {
                ConsoleUi.Error($"Invalid packet: {code}");
                continue;
            }

            await HandleServer(parsed.Value.Command, parsed.Value.Payload);
        }
    }

    private async Task HandleServer(Command cmd, byte[] payload)
    {
        //TODO: ClientHandlers
        switch (cmd)
        {
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