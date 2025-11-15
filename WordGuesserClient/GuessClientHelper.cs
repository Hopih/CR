using System.Net.Sockets;
using WordGuesserServer;

namespace WordGuesserClient;

public class GuessClientHelper(Socket socket)
{
    public Task SendCreate() =>
        Send(GuessCommandHelper.Create());

    public Task SendJoin(Guid sessionId) =>
        Send(GuessCommandHelper.Join(sessionId));

    public Task SendWord(Guid sessionId, string word) =>
        Send(GuessCommandHelper.Word(sessionId, word));

    public Task SendGuess(Guid sessionId, string attempt) =>
        Send(GuessCommandHelper.Guess(sessionId, attempt));

    public Task SendSwap() =>
        Send(GuessCommandHelper.Swap());

    public Task SendEnd() =>
        Send(GuessCommandHelper.End());

    private Task Send(byte[] pkg) =>
        socket.SendAsync(pkg, SocketFlags.None);
}