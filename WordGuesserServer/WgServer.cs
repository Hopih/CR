using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserServer;

public sealed class WgServer : IAsyncDisposable
{
    private readonly Socket _server;
    private readonly GameSessionRegistry _sessions = new();
    private readonly ConcurrentDictionary<Socket, Task> _connections = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly object _lifecycle = new();
    private Task? _run;
    private bool _stopped;
    private bool _disposed;

    public IPEndPoint EndPoint { get; }
    public int SessionCount => _sessions.Count;
    public int ConnectionCount => _connections.Count;

    public WgServer(IPEndPoint endPoint)
    {
        _server = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            _server.Bind(endPoint);
            _server.Listen(32);
            EndPoint = (IPEndPoint)_server.LocalEndPoint!;
        }
        catch
        {
            _server.Dispose();
            throw;
        }
    }

    public Task StartServer(CancellationToken ct = default)
    {
        lock (_lifecycle)
        {
            if (_stopped) throw new InvalidOperationException("Server has stopped.");
            if (_run != null) throw new InvalidOperationException("Server is already running.");
            return _run = RunAsync(ct);
        }
    }

    public async Task StopAsync()
    {
        Task? run;
        lock (_lifecycle)
        {
            _stopped = true;
            if (!_disposed) _stop.Cancel();
            _server.Dispose();
            run = _run;
        }
        if (run != null) await run;
    }

    private async Task RunAsync(CancellationToken externalToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, externalToken);
        var ct = lifetime.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var connection = await _server.AcceptAsync(ct);
                var task = HandleConnection(connection, ct);
                _connections[connection] = task;
                _ = task.ContinueWith(completed => { _connections.TryRemove(connection, out _); },
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (ct.IsCancellationRequested) { }
        catch (SocketException) when (ct.IsCancellationRequested) { }
        finally
        {
            lifetime.Cancel();
            _server.Dispose();
            // No further accepts can add connections after this point.
            var remaining = _connections.ToArray();
            foreach (var (socket, _) in remaining) socket.Dispose();
            await Task.WhenAll(remaining.Select(pair => pair.Value));
            _connections.Clear();
        }
    }

    private async Task HandleConnection(Socket connection, CancellationToken ct)
    {
        try
        {
            using (var greeting = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                greeting.CancelAfter(TimeSpan.FromSeconds(5));
                await connection.SendOk(greeting.Token);
            }
            while (!ct.IsCancellationRequested)
            {
                var packet = await connection.ReceivePacketAsync(ct);
                if (packet == null) break;
                using var commandTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                commandTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var commandToken = commandTimeout.Token;
                try
                {
                    GuessCommandHelper.Decode(packet.Value.Command, packet.Value.Payload);
                }
                catch (FormatException)
                {
                    await connection.SendResponseCommand(CommandResponse.PackageIncorrect, commandToken);
                    continue;
                }
                try
                {
                    await _sessions.ExecuteAsync(packet.Value.Command, connection, packet.Value.Payload, commandToken);
                }
                catch (NotSupportedException)
                {
                    await connection.SendResponseCommand(CommandResponse.CommandIncorrect, commandToken);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or InvalidDataException or SocketException or ObjectDisposedException)
        {
            Console.Error.WriteLine($"Connection closed: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Keep a failed handler observable without taking down unrelated connections.
            Console.Error.WriteLine($"Connection handler failed: {ex}");
        }
        finally
        {
            connection.Dispose();
            await _sessions.DisconnectAsync(connection, notifyOpponent: !ct.IsCancellationRequested);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        lock (_lifecycle)
        {
            if (_disposed) return;
            _disposed = true;
            _stop.Dispose();
        }
    }
}
