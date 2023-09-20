namespace TcpServerExample;

public class Message
{
    public string Autor { get; set; }
    public bool IsClosedMessage { get; set; }
    public string? Data { get; set; }
    public string? FromTo { get; set; }
}