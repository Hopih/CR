using System.Linq.Expressions;
using System.Reflection;

namespace SimpleHttpListenerServer.Core.MiniOrm;

public sealed class ExpressionToSqlVisitor : ExpressionVisitor
{
    private readonly Stack<string> _stack = new();
    private readonly List<(string Name, object? Value)> _parameters = [];
    private int _paramIndex = 0;

    public string Translate(Expression expression)
    {
        Visit(expression);
        return _stack.Pop();
    }
    
    public (string Sql, IReadOnlyList<(string, object?)> Params) TranslateWithParams(Expression expression)
    {
        Visit(expression);
        return (_stack.Pop(), _parameters);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);

        var right = _stack.Pop();
        var left = _stack.Pop();
        var op = GetSqlOperator(node.NodeType);

        _stack.Push($"({left} {op} {right})");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is PropertyInfo prop &&
            prop.DeclaringType?.GetCustomAttribute<TableAttribute>() is not null)
        {
            var col = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
            _stack.Push(col);
        }
        else
        {
            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            AddParameter(value);
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddParameter(node.Value);
        return node;
    }

    private void AddParameter(object? value)
    {
        var name = $"p{_paramIndex++}";
        _parameters.Add((name, value));
        _stack.Push($"@{name}");
    }

    private static string GetSqlOperator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "!=",
        ExpressionType.GreaterThan => ">",
        ExpressionType.LessThan => "<",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.AndAlso => "AND",
        ExpressionType.OrElse => "OR",
        _ => throw new NotSupportedException($"Unsupported operator: {type}")
    };
}