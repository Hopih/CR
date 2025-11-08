using System.Reflection;

namespace WordGuesserServer;

public static class CommandHandlerFactory
{
    private static readonly Lazy<Dictionary<Command, ICommandHandler>> CommandHandlers =
        new(BuildAllHandlers);

    private static Dictionary<Command, ICommandHandler> BuildAllHandlers()
    {
        var allHandlers = new Dictionary<Command, ICommandHandler>();
        var handlerTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => 
            type is { IsClass: true, IsAbstract: false } 
            && typeof(ICommandHandler).IsAssignableFrom(type));

        foreach (var handlerType in handlerTypes)
        {
            var command = handlerType.GetCustomAttribute<CommandAttribute>()!.Command ;
            var handler = (ICommandHandler)Activator.CreateInstance(handlerType)!;
            
            allHandlers.Add(command, handler);
        }
        return allHandlers;
    }

    public static ICommandHandler GetHandler(Command command)
    {
        return CommandHandlers.Value.TryGetValue(command, out var handler) 
            ? handler 
            : throw new NotSupportedException($"No handler found for command {command}");
    }
}