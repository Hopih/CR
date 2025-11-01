namespace WordGuesserServer;

public static class GuessPackageParser
{
    public static (Command Command, byte[] Payload)? TryParse(ReadOnlySpan<byte> data, out CommandResponse commandResponse)
    {
        if (data.Length < 4 || data[0] != GuessPackageMeta.Start || data[^1] != GuessPackageMeta.End)
        {
            commandResponse = CommandResponse.PackageIncorrect;
            return null;
        }

        var length = 0;
        var hasCommand = false;
        
        foreach (var en in Enum.GetValues<Command>().Select(en => (byte)en))
        {
            if (en != data[GuessPackageMeta.CommandByteIndex]) continue;
            length = data[GuessPackageMeta.LengthByteIndex];
            hasCommand = true;
            break;
        }

        if (!hasCommand)
        {
            commandResponse = CommandResponse.CommandIncorrect;
            return null;
        }
        
        if (length > GuessPackageMeta.PayloadMaxByte || length + 4 != data.Length)
        {
            commandResponse = CommandResponse.PackageIncorrect;
            return null;
        }

        var command = (Command)data[GuessPackageMeta.CommandByteIndex];
        
        var payload = data.Slice(GuessPackageMeta.PackagePayloadIndex, length).ToArray();

        commandResponse = CommandResponse.Ok;
        return (command, payload);
    }
}