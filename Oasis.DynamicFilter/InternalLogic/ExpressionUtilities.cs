namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Linq.Expressions;

public static class ExpressionUtilities
{
    public static void BuildExpression<TEntity>(object value, ParameterExpression parameter, string propertyName, ref Expression<Func<TEntity, bool>>? result)
        where TEntity : class
    {
        var exp = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Expression.Constant(value), Expression.Property(parameter, propertyName)), parameter);
        result = result == null ? exp : Expression.Lambda<Func<TEntity, bool>>(Expression.And(result.Body, exp.Body), parameter);
    }
}
