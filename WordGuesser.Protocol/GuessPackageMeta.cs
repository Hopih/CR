namespace WordGuesser.Protocol;

public class GuessPackageMeta
{
    public const int Start = 0x2;
    public const int End = 0x3;
    public const int PayloadMaxByte = 96;
    public const int CommandByteIndex = 1;
    public const int LengthByteIndex = 2;
    public const int PackagePayloadIndex = 3;
}