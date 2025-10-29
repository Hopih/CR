using System.Data;
using System.Reflection;

namespace SimpleHttpListenerServer.Core.MiniOrm;

public static class DataMapper
{
    public static T Map<T>(IDataRecord record) where T : new()
    {
        var obj = new T();
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var col = prop.GetCustomAttribute<ColumnAttribute>()?.Name;
            if (col is null) continue;

            var ordinal = record.GetOrdinal(col);
            if (ordinal < 0 || record.IsDBNull(ordinal)) continue;

            var value = record.GetValue(ordinal);
            prop.SetValue(obj, Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
        }
        return obj;
    }

    public static List<T> MapList<T>(IDataReader reader) where T : new()
    {
        var list = new List<T>();
        while (reader.Read())
            list.Add(Map<T>(reader));
        return list;
    }
}