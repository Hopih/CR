using MongoDB.Driver;

namespace HttpListenerExample;

public class MongoDb
{
    private static readonly string _connectionString = "mongodb://mongo:mongo@localhost:27017/?authMechanism=DEFAULT";

    private static readonly Lazy<MongoClient> _lazyClient = new(() => new MongoClient(_connectionString));

    public static MongoClient Client => _lazyClient.Value;
    
    
}