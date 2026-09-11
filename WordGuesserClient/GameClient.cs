using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserClient;

public sealed class GameClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly IClientUi _ui;
    private readonly Socket _socket;
    private string? _textCommand;
    private bool _started;

    public Guid? SessionId { get; internal set; }
    public PlayerRole? Role { get; internal set; }
    public ClientState State { get; internal set; } = ClientState.Connecting;
    public Command? Pending { get; internal set; }
    public string Mask { get; internal set; } = "";
    public int AttemptsLeft { get; internal set; }
    public int YourScore { get; internal set; }
    public int OpponentScore { get; internal set; }
    public bool HasError { get; private set; }

    public GameClient(string host, int port, IClientUi? ui = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        _host = host;
        _port = port;
        _ui = ui ?? new ConsoleUi();
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task Start(CancellationToken ct = default)
    {
        if (_started) throw new InvalidOperationException("Клиент уже запускался.");
        _started = true;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task<string?>? input = null;
        Task<(Command Command, byte[] Payload)?>? incoming = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _socket.ConnectAsync(_host, _port, timeout.Token);
            Write("Подключение установлено. Ожидаем ответ сервера.");
            while (State != ClientState.Finished)
            {
                input ??= _ui.ReadLineAsync(lifetime.Token).AsTask();
                incoming ??= _socket.ReceivePacketAsync(lifetime.Token);
                await Task.WhenAny(input, incoming).WaitAsync(lifetime.Token);
                // Process server events first when both are ready, so input uses the latest role.
                if (incoming.IsCompleted)
                {
                    var packet = await incoming;
                    incoming = null;
                    if (packet == null) { Write("Сервер закрыл соединение."); break; }
                    GuessCommandHelper.Decode(packet.Value.Command, packet.Value.Payload);
                    await ClientCommandHandlerFactory.Get(packet.Value.Command).Handle(this, packet.Value.Payload);
                    _textCommand = null;
                    if (State != ClientState.Finished) ShowHelp();
                }
                else
                {
                    var line = await input;
                    input = null;
                    if (line == null) { await LeaveAsync(); break; }
                    try { await HandleInput(line.Trim(), lifetime.Token); }
                    catch (Exception ex) when (ex is ArgumentException or FormatException)
                    { Write($"Ошибка ввода: {ex.Message}"); }
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested) await LeaveAsync();
            else { HasError = true; Write("Истекло время ожидания сети."); }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or FormatException or InvalidDataException or NotSupportedException)
        { HasError = true; Write($"Соединение завершено: {ex.Message}"); }
        finally
        {
            State = ClientState.Finished;
            Pending = null;
            SessionId = null;
            Role = null;
            lifetime.Cancel();
            _socket.Dispose();
            await Observe(input);
            await Observe(incoming);
        }
    }

    private static async Task Observe(Task? task)
    {
        if (task == null) return;
        try { await task; }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException or ObjectDisposedException or InvalidDataException) { }
    }

    private async Task HandleInput(string line, CancellationToken ct)
    {
        if (line.Length == 0) return;
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var verb = parts[0].ToLowerInvariant();
        if (verb == "exit") { await LeaveAsync(); State = ClientState.Finished; return; }
        if (verb == "help") { ShowHelp(); return; }
        if (verb == "status") { Write($"Счёт: {YourScore}:{OpponentScore}. Маска: {Mask}. Попыток: {AttemptsLeft}."); ShowHelp(); return; }
        if (Pending != null) { Write("Ожидаем подтверждение предыдущей команды."); return; }
        if (_textCommand != null)
        {
            verb = _textCommand;
            parts = [verb, line];
            _textCommand = null;
        }

        byte[] packet;
        Command command;
        switch (verb)
        {
            case "create" when State == ClientState.Lobby && parts.Length == 1:
                packet = GuessCommandHelper.Create(); command = Command.Create; break;
            case "join" when State == ClientState.Lobby:
                if (parts.Length != 2 || !Guid.TryParse(parts[1], out var id))
                { Write("Формат: join <GUID сессии>"); return; }
                packet = GuessCommandHelper.Join(id); command = Command.Join; break;
            case "word" when State == ClientState.SettingWord:
            case "guess" when State == ClientState.Guessing:
                if (parts.Length == 1)
                {
                    _textCommand = verb;
                    Write(verb == "word" ? "Введите слово:" : "Введите попытку:");
                    return;
                }
                if (SessionId == null) throw new FormatException("Сессия не подтверждена сервером.");
                if (verb == "word") { packet = GuessCommandHelper.Word(SessionId.Value, parts[1]); command = Command.Word; }
                else { packet = GuessCommandHelper.Guess(SessionId.Value, parts[1]); command = Command.Guess; }
                break;
            default:
                Write("Команда недоступна или введена неверно."); ShowHelp(); return;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await _socket.SendPacketAsync(packet, timeout.Token);
        Pending = command;
    }

    private async Task LeaveAsync()
    {
        if (!_socket.Connected || (SessionId == null && Pending is not (Command.Create or Command.Join))) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await _socket.SendPacketAsync(GuessCommandHelper.End(), timeout.Token); }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException) { }
    }

    public void Write(string text) => _ui.WriteLine(text);

    public void ShowHelp()
    {
        if (Pending != null) { Write("Ожидаем ответ сервера. Доступны: status | exit"); return; }
        Write(State switch
        {
            ClientState.Lobby => "Команды: create | join <GUID> | help | exit",
            ClientState.WaitingForPlayer => "Ожидаем второго игрока. Передайте ему GUID сессии. Доступны: status | exit",
            ClientState.SettingWord => "Вы загадываете. Команды: word <слово> | status | exit",
            ClientState.WaitingForWord => "Соперник загадывает слово. Доступны: status | exit",
            ClientState.Guessing => "Вы угадываете. Команды: guess <слово> | status | exit",
            ClientState.Watching => "Ожидаем попытку соперника. Доступны: status | exit",
            ClientState.WaitingForRound => "Ожидаем результат раунда и новые роли. Доступны: status | exit",
            _ => "Ожидаем сервер. Доступны: exit"
        });
    }
}
