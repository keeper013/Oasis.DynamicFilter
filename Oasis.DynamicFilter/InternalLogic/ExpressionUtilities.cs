namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);
    private static readonly IReadOnlyDictionary<FilterByPropertyType, Func<Expression, Expression, BinaryExpression>> _compareFunctions = new Dictionary<FilterByPropertyType, Func<Expression, Expression, BinaryExpression>>
    {
        { FilterByPropertyType.Equality, Expression.Equal },
        { FilterByPropertyType.GreaterThan, Expression.GreaterThan },
        { FilterByPropertyType.GreaterThanOrEqual, Expression.GreaterThanOrEqual },
        { FilterByPropertyType.InEquality, Expression.NotEqual },
        { FilterByPropertyType.LessThan, Expression.LessThan },
        { FilterByPropertyType.LessThanOrEqual, Expression.LessThanOrEqual },
    };

    public static void BuildCompareExpression<TEntityProperty, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, Type?, FilterByPropertyType) data, bool includeNull, bool reverse, ref Expression? result)
    {
        Expression exp = _compareFunctions[reverse ? data.Item3.GetReversed() : data.Item3](
            data.Item1 != null ? Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1!) : Expression.Property(parameter, entityPropertyName),
            data.Item2 != null ? Expression.Convert(Expression.Constant(value, typeof(TFilterProperty)), data.Item2!) : Expression.Constant(value, typeof(TFilterProperty)));
        if (includeNull)
        {
            exp = Expression.Or(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildCollectionContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, FilterByPropertyType, bool) data, bool includeNull, bool reverse, ref Expression? result)
    {
        var collectionType = typeof(ICollection<TEntityPropertyItem>);
        var filterPropertyType = typeof(TFilterProperty);
        var containsMethod = collectionType.GetMethod(nameof(ICollection<TEntityPropertyItem>.Contains))!;
        Expression exp = Expression.And(
            Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, collectionType)),
            Expression.Call(
                Expression.Property(parameter, entityPropertyName),
                containsMethod,
                data.Item1 == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.Item1)));
        if ((data.Item2 == FilterByPropertyType.NotContains) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        if (includeNull)
        {
            exp = Expression.Or(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, collectionType)), exp);
        }
        else if (data.Item3)
        {
            exp = Expression.Condition(
                Expression.Equal(Expression.Constant(value), Expression.Constant(null, typeof(TFilterProperty))),
                Expression.Constant(reverse),
                exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildArrayContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, FilterByPropertyType, bool) data, bool includeNull, bool reverse, ref Expression? result)
    {
        var entityPropertyItemType = typeof(TEntityPropertyItem);
        var filterPropertyType = typeof(TFilterProperty);
        var arrayType = typeof(TEntityPropertyItem[]);
        var containsMethod = EnumerableContains.MakeGenericMethod(entityPropertyItemType);
        Expression exp = Expression.And(
            Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, arrayType)),
            Expression.Call(
                containsMethod,
                Expression.Property(parameter, entityPropertyName),
                data.Item1 == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.Item1)));
        if ((data.Item2 == FilterByPropertyType.NotContains) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        if (includeNull)
        {
            exp = Expression.Or(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, arrayType)), exp);
        }
        else if (data.Item3)
        {
            exp = Expression.Condition(
                Expression.Equal(Expression.Constant(value), Expression.Constant(null, typeof(TFilterProperty))),
                Expression.Constant(reverse),
                exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInCollectionExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, ICollection<TFilterPropertyItem> value, (Type?, FilterByPropertyType, bool) data, bool includeNull, bool reverse, ref Expression? result)
    {
        var collectionType = typeof(ICollection<TFilterPropertyItem>);
        var containsMethod = collectionType.GetMethod(nameof(ICollection<TFilterPropertyItem>.Contains))!;
        Expression exp = Expression.And(
            Expression.NotEqual(Expression.Constant(value), Expression.Constant(null, collectionType)),
            Expression.Call(
                Expression.Constant(value),
                containsMethod,
                data.Item1 == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1)));
        if ((data.Item2 == FilterByPropertyType.NotIn) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        if (includeNull)
        {
            exp = Expression.Or(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
        }
        else if (data.Item3)
        {
            exp = Expression.Condition(
                Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))),
                Expression.Constant(reverse),
                exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInArrayExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, TFilterPropertyItem[] value, (Type?, FilterByPropertyType, bool) data, bool includeNull, bool reverse, ref Expression? result)
    {
        var filterPropertyItemType = typeof(TFilterPropertyItem);
        var containsMethod = EnumerableContains.MakeGenericMethod(filterPropertyItemType);
        Expression exp = Expression.And(
            Expression.NotEqual(Expression.Constant(value), Expression.Constant(null, typeof(TFilterPropertyItem[]))),
            Expression.Call(
                containsMethod,
                Expression.Constant(value),
                data.Item1 == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1)));
        if ((data.Item2 == FilterByPropertyType.NotIn) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        if (includeNull)
        {
            exp = Expression.Or(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
        }
        else if (data.Item3)
        {
            exp = Expression.Condition(
                Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))),
                Expression.Constant(reverse),
                exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    private static FilterByPropertyType GetReversed(this FilterByPropertyType filterType)
    {
        switch (filterType)
        {
            case FilterByPropertyType.Contains:
                return FilterByPropertyType.NotContains;
            case FilterByPropertyType.Equality:
                return FilterByPropertyType.InEquality;
            case FilterByPropertyType.GreaterThan:
                return FilterByPropertyType.LessThanOrEqual;
            case FilterByPropertyType.GreaterThanOrEqual:
                return FilterByPropertyType.LessThan;
            case FilterByPropertyType.In:
                return FilterByPropertyType.NotIn;
            case FilterByPropertyType.InEquality:
                return FilterByPropertyType.Equality;
            case FilterByPropertyType.LessThan:
                return FilterByPropertyType.GreaterThanOrEqual;
            case FilterByPropertyType.LessThanOrEqual:
                return FilterByPropertyType.GreaterThan;
            case FilterByPropertyType.NotContains:
                return FilterByPropertyType.Contains;
            default:
                return FilterByPropertyType.In;
        }
    }
}
