using System.Text;
using WordGuesser.Protocol;

namespace WordGuesserTests;

public class CommandPayloadTests
{
    [Theory]
    [InlineData("КОШКА")]
    [InlineData("ёжик")]
    [InlineData("HELLO")]
    public void Text_Commands_Roundtrip_Utf8(string word)
    {
        var id = Guid.NewGuid();
        foreach (var packet in new[] { GuessCommandHelper.Word(id, word), GuessCommandHelper.Guess(id, word) })
        {
            var parsed = GuessPackageParser.TryParse(packet, out _)!.Value;
            Assert.Equal(37 + Encoding.UTF8.GetByteCount(word), packet[GuessPackageMeta.LengthByteIndex]);
            Assert.Equal((id, word), GuessCommandHelper.DecodeSessionWithData(parsed.Command, parsed.Payload));
        }

        foreach (var packet in new[] { GuessCommandHelper.Win(word), GuessCommandHelper.Result(word), GuessCommandHelper.ReadyToGuess(word) })
        {
            var parsed = GuessPackageParser.TryParse(packet, out _)!.Value;
            Assert.Equal(word, GuessCommandHelper.Decode<string>(parsed.Command, parsed.Payload));
        }
    }

    [Fact]
    public void Payload_Limit_Is_Measured_In_Bytes()
    {
        var id = Guid.NewGuid();
        var word = new string('Я', 29) + "A";
        var packet = GuessCommandHelper.Word(id, word);
        Assert.Equal(96, packet[GuessPackageMeta.LengthByteIndex]);
        Assert.Throws<ArgumentException>(() => GuessCommandHelper.Word(id, new string('Я', 30)));
        Assert.Equal(96, GuessCommandHelper.Win(new string('Я', 48))[GuessPackageMeta.LengthByteIndex]);
        Assert.Throws<ArgumentException>(() => GuessCommandHelper.Win(new string('Я', 49)));
    }

    [Theory]
    [InlineData(PlayerRole.Hosted, 0xE8, 0x03)]
    [InlineData(PlayerRole.Guesser, 0xD0, 0x07)]
    public void Role_Uses_Explicit_Little_Endian(PlayerRole role, byte low, byte high)
    {
        var packet = GuessPackageParser.TryParse(GuessCommandHelper.Ready(role), out _)!.Value;
        Assert.Equal(new byte[] { low, high, 0, 0 }, packet.Payload);
        Assert.Equal(role, GuessCommandHelper.Decode<PlayerRole>(Command.Ready, packet.Payload));
    }

    public static IEnumerable<object[]> InvalidPayloads()
    {
        yield return [Command.Create, new byte[] { 1 }];
        yield return [Command.Swap, new byte[] { 1 }];
        yield return [Command.End, new byte[] { 1 }];
        yield return [Command.CommandResponse, Array.Empty<byte>()];
        yield return [Command.CommandResponse, new byte[] { 0, 0 }];
        yield return [Command.CommandResponse, new byte[] { 255 }];
        yield return [Command.Ready, new byte[] { 1 }];
        yield return [Command.Ready, new byte[4]];
        yield return [Command.Join, Encoding.UTF8.GetBytes("not-a-guid")];
        yield return [Command.SessionId, Array.Empty<byte>()];
        yield return [Command.Word, Encoding.UTF8.GetBytes("word-without-session")];
        yield return [Command.Guess, Encoding.UTF8.GetBytes($"{Guid.NewGuid()}: ")];
        yield return [Command.Word, Encoding.UTF8.GetBytes($"{Guid.NewGuid()}:A:B")];
        yield return [Command.Win, new byte[] { 0xC3, 0x28 }];
        yield return [Command.GuessResult, Array.Empty<byte>()];
        yield return [Command.ReadyToGuess, Encoding.UTF8.GetBytes(" ")];
        yield return [Command.Win, new byte[97]];
        yield return [(Command)255, Array.Empty<byte>()];
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public void Invalid_Payloads_Fail_Explicitly(Command command, byte[] payload) =>
        Assert.Throws<FormatException>(() => GuessCommandHelper.Decode(command, payload));

    [Fact]
    public void Wrong_Decoder_Type_Does_Not_Silently_Return_Default() =>
        Assert.Throws<FormatException>(() => GuessCommandHelper.Decode<Guid>(Command.Win, "HELLO"u8.ToArray()));

    [Fact]
    public void Out_Of_Range_Response_Is_Not_Truncated_To_Ok() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GuessCommandHelper.CommandResponse((CommandResponse)256));
}
