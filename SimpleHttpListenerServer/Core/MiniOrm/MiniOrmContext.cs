using System.Reflection;
using Npgsql;

namespace SimpleHttpListenerServer.Core.MiniOrm;

public sealed class MiniOrmContext(NpgsqlDataSource dataSource)
{
    
    public QueryBuilder<T> From<T>() where T : new() 
        => new QueryBuilder<T>();

    public async Task<List<T>> QueryAsync<T>(QueryBuilder<T> builder, CancellationToken ct = default) where T : new()
    {
        var (sql, parameters) = builder.Build();
        await using var cmd = dataSource.CreateCommand(sql);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return DataMapper.MapList<T>(reader);
    }
    
    public async Task InsertAsync<T>(T entity, CancellationToken ct = default)
    {
        var sql = SqlBuilder.BuildInsert(entity);
        await using var cmd = dataSource.CreateCommand(sql);
        AddParameters(cmd, entity);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync<T>(T entity, CancellationToken ct = default)
    {
        var sql = SqlBuilder.BuildUpdate<T>();
        await using var cmd = dataSource.CreateCommand(sql);
        AddParameters(cmd, entity);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync<T>(object key, CancellationToken ct = default)
    {
        var sql = SqlBuilder.BuildDelete<T>();
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(GetKeyName<T>(), key);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameters<T>(NpgsqlCommand cmd, T entity)
    {
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var col = prop.GetCustomAttribute<ColumnAttribute>()?.Name;
            if (col is null) continue;
            var val = prop.GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue(col, val);
        }
    }

    private static string GetKeyName<T>()
    {
        var key = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                  ?? throw new InvalidOperationException($"No [Key] on {typeof(T).Name}");
        return key.GetCustomAttribute<ColumnAttribute>()?.Name ?? key.Name;
    }
}