using System.Net.Sockets;

namespace WordGuesserServer;

public class Player
{
    public required Socket Connector { get; set; }
    public int WinCount { get; set; }
    public PlayerRole Role { get; set; }
}