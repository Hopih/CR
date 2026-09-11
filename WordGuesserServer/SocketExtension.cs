using WordGuesser.Protocol;
using System.Net.Sockets;

namespace WordGuesserServer;

public static class SocketExtension
{

    public static async Task SendOk(this Socket socket, CancellationToken ct = default)
    {
        await socket.SendResponseCommand(CommandResponse.Ok, ct);
    }
    
    public static async Task SendPlayerNotJoined(this Socket socket, CancellationToken ct = default)
    {
        await socket.SendResponseCommand(CommandResponse.PlayerNotJoined, ct);
    }
    
    public static async Task SendSessionNotFound(this Socket socket, CancellationToken ct = default)
    {
        await socket.SendResponseCommand(CommandResponse.SessionNotFound, ct);
    }
    
    public static async Task SendIncorrectSender(this Socket socket, CancellationToken ct = default)
    {
        await socket.SendResponseCommand(CommandResponse.IncorrectSender, ct);
    }
    
    public static async Task SendReadyToGuess(this Socket socket, string mask, CancellationToken ct = default)
    {
        await socket.SendPacketAsync(GuessCommandHelper.ReadyToGuess(mask),
            ct);
    }
    
    public static async Task SendSessionId(this Socket socket, Guid sessionId, CancellationToken ct = default)
    {
        await socket.SendPacketAsync(GuessCommandHelper.SendSessionId(sessionId),
            ct);
    }

    public static async Task SendResponseCommand(this Socket socket, CommandResponse commandResponse, CancellationToken ct = default)
    {
        await socket.SendPacketAsync(GuessCommandHelper.CommandResponse(commandResponse),
            ct);
    }
}
