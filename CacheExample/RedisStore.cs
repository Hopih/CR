using StackExchange.Redis;

namespace CacheExample;

public static class RedisStore
{
    private static readonly Lazy<ConnectionMultiplexer> LazyConnection = new (
        () => ConnectionMultiplexer.Connect(new ConfigurationOptions 
        { 
            EndPoints =
            {
                "localhost:6379"
            } ,
            Password = "redispw"
        }));
    
    public static ConnectionMultiplexer Connection => LazyConnection.Value;

    public static IDatabase RedisCache => Connection.GetDatabase();
}