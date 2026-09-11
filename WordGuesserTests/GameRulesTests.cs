using WordGuesser.Protocol;
using WordGuesserServer;
using System.Net.Sockets;

namespace WordGuesserTests;

public class GameRulesTests
{
    [Theory]
    [InlineData("ёжик", true)]
    [InlineData("Hello", true)]
    [InlineData("", false)]
    [InlineData("К ОТ", false)]
    [InlineData("кот!", false)]
    [InlineData("123", false)]
    [InlineData("猫", false)]
    [InlineData("😀", false)]
    public void Only_Russian_And_English_Letters_Are_Allowed(string word, bool expected) =>
        Assert.Equal(expected, GameRules.IsWord(word));

    [Fact]
    public void Masks_Are_Not_Accumulated_And_Completed_Round_Rejects_Further_Guesses()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var session = new GameSession { Id = Guid.NewGuid(), Hosted = new Player { Connector = socket }, State = SessionState.Ready };
        Assert.Equal(CommandResponse.Ok, GameRules.SetWord(session, "кот"));
        Assert.Equal(CommandResponse.Ok, GameRules.Guess(session, "кит", out var first, out _));
        Assert.Equal("К*Т", first);
        Assert.Equal(CommandResponse.Ok, GameRules.Guess(session, "дом", out var second, out _));
        Assert.Equal("*О*", second);
        Assert.Equal(CommandResponse.Ok, GameRules.Guess(session, "КОТ", out _, out var won));
        Assert.True(won);
        Assert.Equal(CommandResponse.InvalidState, GameRules.Guess(session, "КОТ", out _, out _));
        Assert.Equal(2, session.TryCount);
    }

    [Theory]
    [InlineData(Command.AttemptResult, new byte[] { 5, 65 })]
    [InlineData(Command.RoundResult, new byte[] { 2, 0, 1, 0, 65 })]
    [InlineData(Command.RoundResult, new byte[] { 1, 4, 0, 0, 65 })]
    [InlineData(Command.MatchResult, new byte[] { 1, 2, 0 })]
    [InlineData(Command.MatchResult, new byte[] { 0, 3, 3 })]
    public void Malformed_Game_Events_Are_Rejected(Command command, byte[] payload) =>
        Assert.Throws<FormatException>(() => GuessCommandHelper.Decode(command, payload));
}
