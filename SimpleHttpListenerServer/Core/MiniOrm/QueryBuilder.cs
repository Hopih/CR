using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SimpleHttpListenerServer.Core.MiniOrm;

public sealed class QueryBuilder<T> where T : new()
{
    private readonly List<string> _joins = new();
    private readonly List<string> _wheres = new();
    private readonly List<(string, object?)> _params = [];
    private string? _orderBy;

    public string TableName => typeof(T).GetCustomAttribute<TableAttribute>()?.Name
                               ?? throw new InvalidOperationException($"Missing [Table] on {typeof(T).Name}");

    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var visitor = new ExpressionToSqlVisitor();
        var (sql, prms) = visitor.TranslateWithParams(predicate.Body);
        _wheres.Add(sql);
        _params.AddRange(prms);
        return this;
    }

    public QueryBuilder<T> Join<TOther>(Expression<Func<T, TOther, bool>> condition)
    {
        var visitor = new ExpressionToSqlVisitor();
        var (sql, _) = visitor.TranslateWithParams(condition.Body);
        var rightTable = typeof(TOther).GetCustomAttribute<TableAttribute>()?.Name
                         ?? throw new InvalidOperationException($"Missing [Table] on {typeof(TOther).Name}");
        _joins.Add($"JOIN {rightTable} ON {sql}");
        return this;
    }
    
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var col = new ExpressionToSqlVisitor().TranslateWithParams(keySelector.Body).Sql;
        _orderBy = $"ORDER BY {col} DESC";
        return this;
    }

    public (string Sql, IReadOnlyList<(string, object?)> Params) Build()
    {
        var cols = string.Join(", ",
            typeof(T).GetProperties()
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name));

        var sb = new StringBuilder($"SELECT {cols} FROM {TableName} ");
        if (_joins.Count > 0) sb.Append(string.Join(" ", _joins)).Append(' ');
        if (_wheres.Count > 0) sb.Append("WHERE ").Append(string.Join(" AND ", _wheres)).Append(' ');
        if (_orderBy != null) sb.Append(_orderBy);
        return (sb.ToString(), _params);
    }
}