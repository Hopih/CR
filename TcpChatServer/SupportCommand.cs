namespace TcpChatServer;

public class SupportCommand
{
    public const byte Hello = 0x6;
    public const byte Say = 0x7;
    public const byte Bye = 0x1B;
}

public enum SupCommand : byte
{
    Hello = 0x6,
    Say = 0x7, 
    Bye = 0x1B
}