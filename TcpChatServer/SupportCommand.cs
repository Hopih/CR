namespace TcpChatServer;

public enum SupportCommand : byte
{
    Hello = 0x06,
    Say = 0x07, 
    Bye = 0x1B
}