using System.Reflection;
using WordGuesserServer;

namespace WordGuesserClient;

public class ClientCommandHandlerFactory
{
    private static readonly Lazy<Dictionary<Command, IClientCommandHandler>> Handlers =
        new(BuildHandlers);

    private static Dictionary<Command, IClientCommandHandler> BuildHandlers()
    {
        var map = new Dictionary<Command, IClientCommandHandler>();

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        typeof(IClientCommandHandler).IsAssignableFrom(t));

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<ClientCommandAttribute>();
            if (attr == null) continue;

            var handler = (IClientCommandHandler)Activator.CreateInstance(type)!;
            map[attr.Command] = handler;
        }

        return map;
    }

    public static IClientCommandHandler Get(Command cmd) =>
        Handlers.Value.TryGetValue(cmd, out var h)
            ? h
            : throw new NotSupportedException($"Client handler not found for command {cmd}");
}