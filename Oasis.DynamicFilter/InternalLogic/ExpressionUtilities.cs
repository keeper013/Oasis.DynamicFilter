namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);

    public static void BuildExpression<TEntity>(object value, ParameterExpression parameter, string propertyName, ref Expression<Func<TEntity, bool>>? result)
        where TEntity : class
    {
        var exp = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Expression.Constant(value), Expression.Property(parameter, propertyName)), parameter);
        result = result == null ? exp : Expression.Lambda<Func<TEntity, bool>>(Expression.And(result.Body, exp.Body), parameter);
    }

    public static void BuildExpressionContains<TEntity, TPropertyType>(object value, ParameterExpression parameter, string propertyName, ref Expression<Func<TEntity, bool>>? result)
    {
        var containsMethod = typeof(ICollection<TPropertyType>).GetMethod(nameof(ICollection<TPropertyType>.Contains))!;
        var exp = Expression.Lambda<Func<TEntity, bool>>(Expression.Call(Expression.Constant(value), containsMethod, Expression.Property(parameter, propertyName)), parameter);
        result = result == null ? exp : Expression.Lambda<Func<TEntity, bool>>(Expression.And(result.Body, exp.Body), parameter);
    }

    public static void BuildExpressionArrayContains<TEntity, TPropertyType>(object value, ParameterExpression parameter, string propertyName, ref Expression<Func<TEntity, bool>>? result)
    {
        var propertyType = typeof(TPropertyType);
        var containsMethod = EnumerableContains.MakeGenericMethod(propertyType);
        var exp = Expression.Lambda<Func<TEntity, bool>>(Expression.Call(containsMethod, Expression.Constant(value), Expression.Property(parameter, propertyName)), parameter);
        result = result == null ? exp : Expression.Lambda<Func<TEntity, bool>>(Expression.And(result.Body, exp.Body), parameter);
    }
}
