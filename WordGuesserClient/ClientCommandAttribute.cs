using WordGuesserServer;

namespace WordGuesserClient;

[AttributeUsage(AttributeTargets.Class)]
public class ClientCommandAttribute(Command command) : Attribute
{
    public Command Command { get; } = command;
}