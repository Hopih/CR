namespace WordGuesserServer;

public enum Command : byte
{
    Create = 0x10,
    SessionId = 0x11,
    Join = 0x20,
    Ready = 0x21,
    Word = 0x30,
    Guess = 0x40,
    Result = 0x41,
    Win = 0x50,
    Swap = 0x51,
    End = 0x52
}