namespace WordGuesserClient;

public interface IClientCommandHandler
{
    Task Handle(GameClient client, byte[] payload);
}