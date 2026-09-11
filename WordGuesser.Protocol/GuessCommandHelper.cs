using System.Buffers.Binary;
using System.Text;

namespace WordGuesser.Protocol;

public static class GuessCommandHelper
{
    private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    private static byte[] BuildPacket(byte[] payload, Command command)
    {
        var packet = new GuessPackageBuilder(payload, command).Build();
        Decode(command, payload);
        return packet;
    }

    private static byte[] Encode(string text)
        => TextEncoding.GetBytes(text);

    private static byte[] Encode(Guid id)
        => Encode(id.ToString("D"));
    
    private static byte[] Encode(PlayerRole role)
    {
        var bytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)role);
        return bytes;
    }

    public static (Guid SessionId, string Data) DecodeSessionWithData(Command command, byte[] payload)
    {
        if (command is not (Command.Word or Command.Guess))
            throw new ArgumentException("Expected Word or Guess command.", nameof(command));

        var text = Decode<string>(command, payload);
        return (Guid.ParseExact(text[..36], "D"), text[37..]);
    }

    public static byte[] ReadyToGuess(string mask)
        => BuildPacket(Encode(mask), Command.ReadyToGuess);

    public static byte[] Create()
        => BuildPacket([], Command.Create);

    public static byte[] Join(Guid sessionId)
        => BuildPacket(Encode(sessionId), Command.Join);
    
    public static byte[] SendSessionId(Guid sessionId)
        => BuildPacket(Encode(sessionId), Command.SessionId);

    public static byte[] Ready(PlayerRole role)
        => BuildPacket(Encode(role), Command.Ready);

    public static byte[] Word(Guid sessionId, string word)
        => BuildPacket(Encode($"{sessionId}:{word}"), Command.Word);

    public static byte[] Guess(Guid sessionId, string attempt)
        => BuildPacket(Encode($"{sessionId}:{attempt}"), Command.Guess);

    public static byte[] Result(string result)
        => BuildPacket(Encode(result), Command.GuessResult);

    public static byte[] Win(string word)
        => BuildPacket(Encode(word), Command.Win);

    public static byte[] Swap()
        => BuildPacket([], Command.Swap);

    public static byte[] End()
        => BuildPacket([], Command.End);

    public static byte[] CommandResponse(CommandResponse commandResponse)
    {
        if (!Enum.IsDefined(commandResponse))
            throw new ArgumentOutOfRangeException(nameof(commandResponse));
        return BuildPacket([(byte)commandResponse], Command.CommandResponse);
    }
    
    
    public static object? Decode(Command command, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length > GuessPackageMeta.PayloadMaxByte)
            throw new FormatException("Payload exceeds 96 bytes.");

        switch (command)
        {
            case Command.AttemptResult:
            case Command.RoundResult:
            case Command.MatchResult:
                return GameEventCodec.Decode(command, payload);
            case Command.Create:
            case Command.Swap:
            case Command.End:
                if (payload.Length != 0)
                    throw new FormatException("This command must have an empty payload.");
                return null;
            case Command.CommandResponse:
                if (payload.Length != 1 || !Enum.IsDefined((CommandResponse)payload[0]))
                    throw new FormatException("Invalid response code.");
                return (CommandResponse)payload[0];
            case Command.Ready:
                if (payload.Length != sizeof(int))
                    throw new FormatException("A role must contain four bytes.");
                var role = (PlayerRole)BinaryPrimitives.ReadInt32LittleEndian(payload);
                if (!Enum.IsDefined(role))
                    throw new FormatException("Unknown player role.");
                return role;
            case Command.Join:
            case Command.SessionId:
                var id = DecodeText(payload);
                if (payload.Length != 36 || !Guid.TryParseExact(id, "D", out var sessionId))
                    throw new FormatException("Invalid session ID.");
                return sessionId;
            case Command.Word:
            case Command.Guess:
                var text = DecodeText(payload);
                if (text.Length <= 37 || text[36] != ':' ||
                    !Guid.TryParseExact(text[..36], "D", out _) ||
                    string.IsNullOrWhiteSpace(text[37..]) || text[37..].Contains(':'))
                    throw new FormatException("Expected session ID followed by ':' and nonempty text.");
                return text;
            case Command.ReadyToGuess:
            case Command.GuessResult:
            case Command.Win:
                var result = DecodeText(payload);
                if (string.IsNullOrWhiteSpace(result))
                    throw new FormatException("Expected nonempty text.");
                return result;
            default:
                throw new FormatException("Unknown command.");
        }
    }

    private static string DecodeText(byte[] payload)
    {
        try
        {
            return TextEncoding.GetString(payload);
        }
        catch (DecoderFallbackException ex)
        {
            throw new FormatException("Invalid UTF-8 text.", ex);
        }
    }

    public static T Decode<T>(Command command, byte[] payload)
    {
        object? decoded = Decode(command, payload);
        return decoded is T val ? val : throw new FormatException($"Command {command} does not contain {typeof(T).Name}.");
    }
}
