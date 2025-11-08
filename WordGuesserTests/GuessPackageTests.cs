using System.Text;
using WordGuesserServer;

namespace WordGuesserTests;

public class GuessPackageTests
{
    [Fact]
    public void Correct_Parse_Package_With_Create_Command()
    {
        var package = new GuessPackageBuilder([], Command.Create).Build();

        (Command Command, byte[] Payload)? parsed = GuessPackageParser.TryParse(package, out _);
        
        Assert.NotNull(parsed);
        Assert.Equal(Command.Create, parsed.Value.Command);
        Assert.Empty(parsed.Value.Payload);
    }
    
    [Fact]
    public void Parse_Package_With_Payload()
    {
        var data = "HELLO"u8.ToArray();
        var package = new GuessPackageBuilder(data, Command.Word).Build();

        var parsed = GuessPackageParser.TryParse(package, out _);

        Assert.NotNull(parsed);
        Assert.Equal(Command.Word, parsed.Value.Command);
        Assert.Equal("HELLO", Encoding.ASCII.GetString(parsed.Value.Payload));
    }
    
    
    [Fact]
    public void Return_Null_When_Invalid_Boundaries()
    {
        var data = new byte[] { 0x00, (byte)Command.Create, 0x00, 0x00 };
        var parsed = GuessPackageParser.TryParse(data, out _);
        Assert.Null(parsed);
    }
    
    [Fact]
    public void Return_Null_When_Invalid_Length()
    {
        var valid = new GuessPackageBuilder("A"u8.ToArray(), Command.Word).Build();
        valid[GuessPackageMeta.LengthByteIndex] = 10;

        var parsed = GuessPackageParser.TryParse(valid, out _);
        Assert.Null(parsed);
    }
    
    [Fact]
    public void Parse_Maximum_Payload()
    {
        var data = new byte[GuessPackageMeta.PayloadMaxByte];
        new Random().NextBytes(data);

        var package = new GuessPackageBuilder(data, Command.GuessResult).Build();
        var parsed = GuessPackageParser.TryParse(package, out _);

        Assert.NotNull(parsed);
        Assert.Equal(Command.GuessResult, parsed.Value.Command);
        Assert.Equal(data, parsed.Value.Payload);
    }
    
    [Fact]
    public void Throw_When_Payload_Exceeds_Max_Byte()
    {
        var data = new byte[GuessPackageMeta.PayloadMaxByte + 1];
        Assert.Throws<ArgumentException>(() => new GuessPackageBuilder(data, Command.Word));
    }
    
    [Fact]
    public void Builder_Creates_Correct_Byte_Layout()
    {
        var bytes = "AB"u8.ToArray();
        var pkg = new GuessPackageBuilder(bytes, Command.Word).Build();

        Assert.Equal(0x02, pkg[0]);
        Assert.Equal((byte)Command.Word, pkg[1]);
        Assert.Equal(2, pkg[2]);
        Assert.Equal((byte)'A', pkg[3]);
        Assert.Equal((byte)'B', pkg[4]);
        Assert.Equal(0x03, pkg[^1]);
    }
    
    [Theory]
    [InlineData(Command.Create)]
    [InlineData(Command.ReadyToGuess)]
    [InlineData(Command.End)]
    [InlineData(Command.Guess)]
    [InlineData(Command.Join)]
    [InlineData(Command.Ready)]
    [InlineData(Command.Word)]
    [InlineData(Command.GuessResult)]
    public void Build_And_Parse_Returns_Same_Command(Command cmd)
    {
        var pkg = new GuessPackageBuilder([], cmd).Build();
        var parsed = GuessPackageParser.TryParse(pkg, out _);
        Assert.Equal(cmd, parsed?.Command);
    }
}