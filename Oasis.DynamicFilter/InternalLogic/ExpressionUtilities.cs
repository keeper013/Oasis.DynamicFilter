﻿namespace Oasis.DynamicFilter.InternalLogic;

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

    public static void BuildCompareExpression<TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, Type?, FilterByPropertyType) data, bool reverse, ref Expression? result)
    {
        Expression exp = _compareFunctions[reverse ? data.Item3.GetReversed() : data.Item3](
            data.Item1 != null ? Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1!) : Expression.Property(parameter, entityPropertyName),
            data.Item2 != null ? Expression.Convert(Expression.Constant(value, typeof(TFilterProperty)), data.Item2!) : Expression.Constant(value, typeof(TFilterProperty)));
        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildCollectionContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, FilterByPropertyType) data, bool reverse, ref Expression? result)
    {
        var containsMethod = typeof(ICollection<TEntityPropertyItem>).GetMethod(nameof(ICollection<TEntityPropertyItem>.Contains))!;
        Expression exp = Expression.Call(
            Expression.Property(parameter, entityPropertyName),
            containsMethod,
            data.Item1 == null ? Expression.Constant(value, typeof(TFilterProperty)) : Expression.Convert(Expression.Constant(value, typeof(TFilterProperty)), data.Item1));
        if ((data.Item2 == FilterByPropertyType.NotContains) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildArrayContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, (Type?, FilterByPropertyType) data, bool reverse, ref Expression? result)
    {
        var entityPropertyItemType = typeof(TEntityPropertyItem);
        var containsMethod = EnumerableContains.MakeGenericMethod(entityPropertyItemType);
        Expression exp = Expression.Call(
            Expression.Property(parameter, entityPropertyName),
            containsMethod,
            data.Item1 == null ? Expression.Constant(value, typeof(TFilterProperty)) : Expression.Convert(Expression.Constant(value, typeof(TFilterProperty)), data.Item1));
        if ((data.Item2 == FilterByPropertyType.NotContains) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInCollectionExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, ICollection<TFilterPropertyItem> value, (Type?, FilterByPropertyType) data, bool reverse, ref Expression? result)
    {
        var containsMethod = typeof(ICollection<TFilterPropertyItem>).GetMethod(nameof(ICollection<TFilterPropertyItem>.Contains))!;
        Expression exp = Expression.Call(
            Expression.Constant(value),
            containsMethod,
            data.Item1 == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1));
        if ((data.Item2 == FilterByPropertyType.NotIn) ^ reverse)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInArrayExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, TFilterPropertyItem[] value, (Type?, FilterByPropertyType) data, bool reverse, ref Expression? result)
    {
        var filterPropertyItemType = typeof(TFilterPropertyItem);
        var containsMethod = EnumerableContains.MakeGenericMethod(filterPropertyItemType);
        Expression exp = Expression.Call(
            Expression.Constant(value),
            containsMethod,
            data.Item1 == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.Item1));
        if ((data.Item2 == FilterByPropertyType.NotIn) ^ reverse)
        {
            exp = Expression.Not(exp);
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
