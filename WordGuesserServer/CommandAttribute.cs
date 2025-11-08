namespace WordGuesserServer;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute(Command command) : Attribute
{
    public Command Command { get; set; } = command;
}