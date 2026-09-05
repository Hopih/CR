using System.Text;

namespace WordGuesserServer;

public static class GuessCommandHelper
{
    
    private static readonly Dictionary<Command, Func<byte[], object?>> Decoders = new()
    {
        { Command.SessionId, payload => Guid.Parse(Encoding.ASCII.GetString(payload)) },
        { Command.Join, payload => Guid.Parse(Encoding.ASCII.GetString(payload)) },
        { Command.Word, payload => Encoding.ASCII.GetString(payload) },
        { Command.Ready , payload => (PlayerRole)BitConverter.ToInt32(payload, 0)},
        { Command.Guess, payload => Encoding.ASCII.GetString(payload) },
        { Command.GuessResult, payload => Encoding.ASCII.GetString(payload) },
        { Command.Win, payload => Encoding.ASCII.GetString(payload) },
        { Command.CommandResponse ,  payload => (CommandResponse)payload[GuessPackageMeta.PackagePayloadIndex] }
    };
    
    
    private static byte[] Encode(string text)
        => Encoding.ASCII.GetBytes(text);

    private static byte[] Encode(Guid id)
        => Encoding.ASCII.GetBytes(id.ToString());
    
    private static byte[] Encode(PlayerRole id)
        => BitConverter.GetBytes((int)id);
    
    private static string DecodeString(byte[] payload)
        => payload.Length == 0 ? string.Empty : Encoding.ASCII.GetString(payload);


    private static (Guid sessionId, string data) DecodeSessionWithData(byte[] payload)
    {
        var result = DecodeString(payload);
        var partOfPayload = result.Split(':');
        
        if (partOfPayload.Length != 2)
        {
            throw new  FormatException("Invalid payload data");
        }
        
        var sessionId = Guid.Parse(partOfPayload[0]);
        var data = partOfPayload[1];
        
        return (sessionId, data);
    }

    private static Guid DecodeGuid(byte[] payload)
        => Guid.Parse(DecodeString(payload));
    

    public static byte[] ReadyToGuess(string mask)
        => new GuessPackageBuilder(Encode(mask), Command.ReadyToGuess).Build();

    public static byte[] Create()
        => new GuessPackageBuilder([], Command.Create).Build();

    public static byte[] Join(Guid sessionId)
        => new GuessPackageBuilder(Encode(sessionId), Command.Join).Build();
    
    public static byte[] SendSessionId(Guid sessionId)
        => new GuessPackageBuilder(Encode(sessionId), Command.SessionId).Build();

    public static byte[] Ready(PlayerRole role)
        => new GuessPackageBuilder(Encode(role), Command.Ready).Build();

    public static byte[] Word(Guid sessionId, string word)
        => new GuessPackageBuilder(Encode($"{sessionId}:{word}"), Command.Word).Build();

    public static byte[] Guess(Guid sessionId, string attempt)
        => new GuessPackageBuilder(Encode($"{sessionId}:{attempt}"), Command.Guess).Build();

    public static byte[] Result(string result)
        => new GuessPackageBuilder(Encode(result), Command.GuessResult).Build();

    public static byte[] Win(string word)
        => new GuessPackageBuilder(Encode(word), Command.Win).Build();

    public static byte[] Swap()
        => new GuessPackageBuilder([], Command.Swap).Build();

    public static byte[] End()
        => new GuessPackageBuilder([], Command.End).Build();

    public static byte[] CommandResponse(CommandResponse commandResponse)
        => new GuessPackageBuilder([(byte)commandResponse], Command.CommandResponse).Build();
    
    
    public static object? Decode(Command command, byte[] payload)
    {
        if (payload.Length == 0)
            return null;

        return Decoders.TryGetValue(command, out var decoder)
                ? decoder(payload)
                : Encoding.ASCII.GetString(payload);
    }
    
    public static T? Decode<T>(Command command, byte[] payload)
    {
        object? decoded = Decode(command, payload);
        return decoded is T val ? val : default;
    }
}