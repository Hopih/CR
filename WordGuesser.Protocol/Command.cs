namespace WordGuesser.Protocol;

public enum Command : byte
{
    ReadyToGuess = 0x05,
    CommandResponse = 0x06,
    Create = 0x10,
    SessionId = 0x11,
    Join = 0x20,
    Ready = 0x21,
    Word = 0x30,
    Guess = 0x40,
    GuessResult = 0x41,
    Win = 0x50,
    Swap = 0x51,
    End = 0x52
}