namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);

    public static void BuildFilterByPropertyExpression<TEntityProperty, TFilterProperty>(FilterByPropertyType filterType, TFilterProperty value, ParameterExpression parameter, string entityPropertyName, bool revert, ref Expression? result)
    {
        if (revert)
        {
            filterType = filterType.GetReversed();
        }

        var entityPropertyType = typeof(TEntityProperty);
        var entityPropertyIsNullable = entityPropertyType.IsNullable(out var entityArgumenType);
        if (entityPropertyIsNullable)
        {
            entityPropertyType = entityArgumenType;
        }

        var filterPropertyType = typeof(TFilterProperty);
        var filterPropertyIsNullable = filterPropertyType.IsNullable(out var filterArgumentType);
        if (filterPropertyIsNullable)
        {
            filterPropertyType = filterArgumentType;
        }

        var right = Expression.Property(parameter, entityPropertyName);
        var left = Expression.Constant(value, filterPropertyType);
        var conversion = GetComparisonConversion(entityPropertyType!, filterPropertyType!);

        Expression exp;
        switch (filterType)
        {
            case FilterByPropertyType.Equality:
                exp = Expression.Equal(left, right);
                break;
            case FilterByPropertyType.GreaterThan:
                exp = Expression.GreaterThan(left, right);
                break;
            case FilterByPropertyType.GreaterThanOrEqual:
                exp = Expression.GreaterThanOrEqual(left, right);
                break;
            case FilterByPropertyType.InEquality:
                exp = Expression.NotEqual(left, right);
                break;
            case FilterByPropertyType.LessThan:
                exp = Expression.LessThan(left, right);
                break;
            default:
                exp = Expression.LessThanOrEqual(left, right);
                break;
        }

        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildCollectionContainsExpression<TEntityPropertyItem, TFilterProperty>(FilterByPropertyType filterType, TFilterProperty value, ParameterExpression parameter, string propertyName, bool revert, ref Expression? result)
    {
        var containsMethod = typeof(ICollection<TEntityPropertyItem>).GetMethod(nameof(ICollection<TEntityPropertyItem>.Contains))!;
        var exp = Expression.Call(Expression.Property(parameter, propertyName), containsMethod, Expression.Convert(Expression.Constant(value), typeof(TEntityPropertyItem)));
        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildArrayContainsExpression<TEntityPropertyItem, TfilterProperty>(FilterByPropertyType filterType, TfilterProperty value, ParameterExpression parameter, string propertyName, bool revert, ref Expression? result)
    {
        var entityPropertyItemType = typeof(TEntityPropertyItem);
        var containsMethod = EnumerableContains.MakeGenericMethod(entityPropertyItemType);
        var exp = Expression.Call(Expression.Property(parameter, propertyName), containsMethod, Expression.Convert(Expression.Constant(value), entityPropertyItemType));
        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInCollectionExpression<TEntityProperty, TFilterPropertyItem>(FilterByPropertyType filterType, ICollection<TFilterPropertyItem> value, ParameterExpression parameter, string propertyName, bool revert, ref Expression? result)
    {
        var containsMethod = typeof(ICollection<TFilterPropertyItem>).GetMethod(nameof(ICollection<TFilterPropertyItem>.Contains))!;
        var exp = Expression.Call(Expression.Constant(value), containsMethod, Expression.Convert(Expression.Property(parameter, propertyName), typeof(TFilterPropertyItem)));
        result = result == null ? exp : Expression.And(result, exp);
    }

    public static void BuildInArrayExpression<TEntityProperty, TFilterPropertyItem>(FilterByPropertyType filterType, TFilterPropertyItem[] value, ParameterExpression parameter, string propertyName, bool revert, ref Expression? result)
    {
        var filterPropertyItemType = typeof(TFilterPropertyItem);
        var containsMethod = EnumerableContains.MakeGenericMethod(filterPropertyItemType);
        var exp = Expression.Call(Expression.Constant(value), containsMethod, Expression.Convert(Expression.Property(parameter, propertyName), filterPropertyItemType));
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
