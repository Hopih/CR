using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace HttpListenerStepByStep.Auth;

public sealed record SessionData(Guid UserId, DateTimeOffset AbsoluteExpiresAt);
public sealed record IssuedSession(string Token, SessionData Data);

public sealed class SessionStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _idle;
    private readonly TimeSpan _lifetime;
    public SessionStore(IMemoryCache cache, TimeSpan? idle = null,
        TimeSpan? lifetime = null)
    {
        _cache = cache;
        _idle = idle ?? TimeSpan.FromMinutes(10);
        
        _lifetime = lifetime ?? TimeSpan.FromMinutes(30);
        
        if (_idle <= TimeSpan.Zero || _lifetime <= TimeSpan.Zero ||
            _idle > _lifetime ||
            _lifetime > TimeSpan.FromMinutes(30))
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }
    }
    public IssuedSession Create(Guid userId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var data = new SessionData(userId, DateTimeOffset.UtcNow.Add(_lifetime));
        
        _cache.Set(Key(token), data, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _idle,
            AbsoluteExpiration = data.AbsoluteExpiresAt,
            Size = 1
        });
        
        return new IssuedSession(token, data);
    }
    public SessionData? Find(string? token)
    {
        if (!ValidToken(token))
        {
            return null;
        }

        if (!_cache.TryGetValue<SessionData>(Key(token!), out var data))
        {
            return null;
        }

        // Дополнительный явный предел, даже при активном обращении к cache.
        if (data is not null && DateTimeOffset.UtcNow < data.AbsoluteExpiresAt)
        {
            return data;
        }

        _cache.Remove(Key(token!)); return null;
    }
    
    public void Remove(string? token)
    {
        if (ValidToken(token)) _cache.Remove(Key(token!));
    }
    public static bool ValidToken(string? value) => 
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
    private static string Key(string token) =>
        $"session:{Convert.ToHexString(SHA256.HashData(Convert.FromHexString(token)))}";
}