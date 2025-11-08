using System.Net.Sockets;

namespace WordGuesserServer;

public static class SocketExtension
{

    public static async Task SendOk(this Socket socket)
    {
        await socket.SendResponseCommand(CommandResponse.Ok);
    }
    
    public static async Task SendPlayerNotJoined(this Socket socket)
    {
        await socket.SendResponseCommand(CommandResponse.PlayerNotJoined);
    }
    
    public static async Task SendSessionNotFound(this Socket socket)
    {
        await socket.SendResponseCommand(CommandResponse.SessionNotFound);
    }
    
    public static async Task SendIncorrectSender(this Socket socket)
    {
        await socket.SendResponseCommand(CommandResponse.IncorrectSender);
    }
    
    public static async Task SendReadyToGuess(this Socket socket, string mask)
    {
        await socket.SendAsync(new GuessPackageBuilder(GuessCommandHelper.ReadyToGuess(mask), 
                Command.ReadyToGuess).Build(),
            SocketFlags.None);
    }

    private static async Task SendResponseCommand(this Socket socket, CommandResponse commandResponse)
    {
        await socket.SendAsync(new GuessPackageBuilder(
                    GuessCommandHelper.CommandResponse(commandResponse), Command.CommandResponse)
                .Build(),
            SocketFlags.None);
    }
}