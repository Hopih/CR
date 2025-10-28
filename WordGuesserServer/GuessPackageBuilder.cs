namespace WordGuesserServer;

public class GuessPackageBuilder
{
    byte[] _package;
    public GuessPackageBuilder(byte[] content, Command command)
    {
        if (content.Length > GuessPackageMeta.PayloadMaxByte)
        {
            throw new ArgumentException("Payload exceed maximum byte");
        }

        _package = new byte[4 + content.Length];
        
        CreateBasePackage(content, command);
    }

    private void CreateBasePackage(byte[] content, Command command)
    {
        _package[0] = GuessPackageMeta.Start;
        _package[^1] = GuessPackageMeta.End;
        _package[GuessPackageMeta.CommandByteIndex] = (byte) command;
        _package[GuessPackageMeta.LengthByteIndex] = (byte) content.Length;
        Array.Copy(content, 0, _package, GuessPackageMeta.PackagePayloadIndex, content.Length);
    }

    public byte[] Build() => _package;
}