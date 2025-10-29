using System.Reflection;

namespace SimpleHttpListenerServer.Core.MiniOrm;

public static class SqlBuilder
{
    public static string BuildSelect<T>()
    {
        var table = typeof(T).GetCustomAttribute<TableAttribute>()?.Name
                    ?? throw new InvalidOperationException($"Class {typeof(T).Name} missing [Table]");

        var columns = typeof(T).GetProperties()
            .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
            .ToArray();

        return $"SELECT {string.Join(", ", columns)} FROM {table}";
    }

    public static string BuildInsert<T>(T entity)
    {
        var table = typeof(T).GetCustomAttribute<TableAttribute>()?.Name
                    ?? throw new InvalidOperationException($"Class {typeof(T).Name} missing [Table]");

        var props = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<KeyAttribute>() == null)
            .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
            .ToArray();

        var cols = props.Select(p => p.GetCustomAttribute<ColumnAttribute>()!.Name);
        var vals = props.Select(p => "@" + p.GetCustomAttribute<ColumnAttribute>()!.Name);

        return $"INSERT INTO {table} ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)})";
    }

    public static string BuildUpdate<T>()
    {
        var table = typeof(T).GetCustomAttribute<TableAttribute>()?.Name
                    ?? throw new InvalidOperationException($"Class {typeof(T).Name} missing [Table]");

        var props = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<KeyAttribute>() == null)
            .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
            .ToArray();

        var setPart = string.Join(", ", props.Select(p =>
            $"{p.GetCustomAttribute<ColumnAttribute>()!.Name} = @{p.GetCustomAttribute<ColumnAttribute>()!.Name}"));

        var key = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                  ?? throw new InvalidOperationException($"No [Key] found on {typeof(T).Name}");

        var keyCol = key.GetCustomAttribute<ColumnAttribute>()?.Name ?? key.Name;

        return $"UPDATE {table} SET {setPart} WHERE {keyCol} = @{keyCol}";
    }

    public static string BuildDelete<T>()
    {
        var table = typeof(T).GetCustomAttribute<TableAttribute>()?.Name
                    ?? throw new InvalidOperationException($"Class {typeof(T).Name} missing [Table]");

        var key = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                  ?? throw new InvalidOperationException($"No [Key] found on {typeof(T).Name}");

        var keyCol = key.GetCustomAttribute<ColumnAttribute>()?.Name ?? key.Name;
        return $"DELETE FROM {table} WHERE {keyCol} = @{keyCol}";
    }
}