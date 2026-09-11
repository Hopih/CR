using System.Text;

namespace WordGuesser.Protocol;

public sealed record AttemptResult(int AttemptsLeft, string Mask);
public sealed record RoundResult(bool Won, int YourScore, int OpponentScore, int AttemptsLeft, string Word);
public sealed record MatchResult(bool Won, int YourScore, int OpponentScore);

public static class GameEventCodec
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static byte[] Attempt(int attemptsLeft, string mask) =>
        Build(Command.AttemptResult, [(byte)Checked(attemptsLeft, 0, 4), .. Utf8.GetBytes(mask)]);

    public static byte[] Round(bool won, int yours, int theirs, int attemptsLeft, string word) =>
        Build(Command.RoundResult, [(byte)(won ? 1 : 0), (byte)Checked(yours, 0, 3),
            (byte)Checked(theirs, 0, 3), (byte)Checked(attemptsLeft, 0, 4), .. Utf8.GetBytes(word)]);

    public static byte[] Match(bool won, int yours, int theirs) =>
        Build(Command.MatchResult, [(byte)(won ? 1 : 0), (byte)Checked(yours, 0, 3), (byte)Checked(theirs, 0, 3)]);

    private static int Checked(int value, int min, int max) => value >= min && value <= max
        ? value : throw new ArgumentOutOfRangeException(nameof(value));

    private static byte[] Build(Command command, byte[] payload)
    {
        Decode(command, payload);
        return new GuessPackageBuilder(payload, command).Build();
    }

    public static object Decode(Command command, byte[] payload)
    {
        if (payload.Length > GuessPackageMeta.PayloadMaxByte) throw new FormatException("Payload too large.");
        if (command == Command.AttemptResult && payload.Length > 1 && payload[0] <= 4)
            return new AttemptResult(payload[0], Text(payload, 1));
        if (command == Command.RoundResult && payload.Length > 4 && payload[0] <= 1 &&
            payload[1] <= 3 && payload[2] <= 3 && payload[3] <= 4)
            return new RoundResult(payload[0] == 1, payload[1], payload[2], payload[3], Text(payload, 4));
        if (command == Command.MatchResult && payload.Length == 3 && payload[0] <= 1 &&
            ((payload[0] == 1 && payload[1] == 3 && payload[2] < 3) ||
             (payload[0] == 0 && payload[2] == 3 && payload[1] < 3)))
            return new MatchResult(payload[0] == 1, payload[1], payload[2]);
        throw new FormatException("Invalid game event.");
    }

    private static string Text(byte[] payload, int offset)
    {
        try
        {
            var text = Utf8.GetString(payload, offset, payload.Length - offset);
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Expected text.");
            return text;
        }
        catch (DecoderFallbackException ex) { throw new FormatException("Invalid UTF-8.", ex); }
    }
}
