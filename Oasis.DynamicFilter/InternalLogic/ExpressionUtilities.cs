namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public record struct CompareData(Type? entityPropertyConvertTo, Type? filterPropertyConvertTo, FilterByPropertyType filterType);
public record struct ContainData(Type? filterPropertyConvertTo, FilterByPropertyType filterType, bool nullValueNotCovered);
public record struct InData(Type? entityPropertyConvertTo, FilterByPropertyType filterType, bool nullValueNotCovered);
public record struct RangeData(CompareData minData, CompareData maxData);

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

    public static void BuildCompareExpression<TEntityProperty, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, CompareData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        Expression exp = _compareFunctions[reverse ? data.filterType.GetReversed() : data.filterType](
            data.entityPropertyConvertTo != null ? Expression.Convert(Expression.Property(parameter, entityPropertyName), data.entityPropertyConvertTo!) : Expression.Property(parameter, entityPropertyName),
            data.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, typeof(TFilterProperty)), data.filterPropertyConvertTo!) : Expression.Constant(value, typeof(TFilterProperty)));

        if (includeNull.HasValue)
        {
            exp = includeNull.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildCollectionContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, ContainData data, bool includeNull, bool reverse, ref Expression? result)
    {
        var collectionType = typeof(ICollection<TEntityPropertyItem>);
        var notContains = (data.filterType == FilterByPropertyType.NotContains) ^ reverse;
        var filterPropertyType = typeof(TFilterProperty);
        var containsMethod = collectionType.GetMethod(nameof(ICollection<TEntityPropertyItem>.Contains))!;
        Expression exp = Expression.Call(
            Expression.Property(parameter, entityPropertyName),
            containsMethod,
            data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
        if (notContains)
        {
            exp = Expression.Not(exp);
        }

        exp = includeNull
            ? data.nullValueNotCovered && value == null
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, collectionType)), Expression.Constant(notContains))
                : Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, collectionType)), exp)
            : data.nullValueNotCovered && value == null
                ? Expression.Constant(notContains)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, collectionType)), exp);

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildArrayContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, ContainData data, bool includeNull, bool reverse, ref Expression? result)
    {
        var arrayType = typeof(TEntityPropertyItem[]);
        var notContains = (data.filterType == FilterByPropertyType.NotContains) ^ reverse;
        var entityPropertyItemType = typeof(TEntityPropertyItem);
        var filterPropertyType = typeof(TFilterProperty);
        var containsMethod = EnumerableContains.MakeGenericMethod(entityPropertyItemType);
        Expression exp = Expression.Call(
            containsMethod,
            Expression.Property(parameter, entityPropertyName),
            data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
        if (notContains)
        {
            exp = Expression.Not(exp);
        }

        exp = includeNull
            ? data.nullValueNotCovered && value == null
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, arrayType)), Expression.Constant(notContains))
                : Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, arrayType)), exp)
            : data.nullValueNotCovered && value == null
                ? Expression.Constant(notContains)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, arrayType)), exp);

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildInCollectionExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, ICollection<TFilterPropertyItem> value, InData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        var notIn = (data.filterType == FilterByPropertyType.NotIn) ^ reverse;
        Expression exp;
        if (value == null || !value.Any())
        {
            exp = includeNull.HasValue && includeNull.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), Expression.Constant(notIn))
                : Expression.Constant(notIn);
        }
        else
        {
            var collectionType = typeof(ICollection<TFilterPropertyItem>);
            var containsMethod = collectionType.GetMethod(nameof(ICollection<TFilterPropertyItem>.Contains))!;
            exp = Expression.Call(
                Expression.Constant(value),
                containsMethod,
                data.entityPropertyConvertTo == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.entityPropertyConvertTo));

            if (notIn)
            {
                exp = Expression.Not(exp);
            }

            if (data.nullValueNotCovered)
            {
                exp = Expression.Condition(
                    Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))),
                    Expression.Constant((includeNull.HasValue && includeNull.Value) || notIn),
                    exp);
            }
            else if (includeNull.HasValue)
            {
                exp = includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
            }
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildInArrayExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, TFilterPropertyItem[] value, InData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        var notIn = (data.filterType == FilterByPropertyType.NotIn) ^ reverse;
        Expression exp;
        if (value == null || !value.Any())
        {
            exp = includeNull.HasValue && includeNull.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), Expression.Constant(notIn))
                : Expression.Constant(notIn);
        }
        else
        {
            var filterPropertyItemType = typeof(TFilterPropertyItem);
            var containsMethod = EnumerableContains.MakeGenericMethod(filterPropertyItemType);
            exp = Expression.AndAlso(
            Expression.NotEqual(Expression.Constant(value), Expression.Constant(null, typeof(TFilterPropertyItem[]))),
            Expression.Call(
                containsMethod,
                Expression.Constant(value),
                data.entityPropertyConvertTo == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.entityPropertyConvertTo)));

            if (notIn)
            {
                exp = Expression.Not(exp);
            }

            if (data.nullValueNotCovered)
            {
                exp = Expression.Condition(
                    Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))),
                    Expression.Constant((includeNull.HasValue && includeNull.Value) || notIn),
                    exp);
            }
            else if (includeNull.HasValue)
            {
                exp = includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
            }
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildFilterRangeExpression<TEntityProperty, TFilterMinProperty, TFilterMaxProperty>(ParameterExpression parameter, string entityPropertyName, TFilterMinProperty min, TFilterMaxProperty max, RangeData data, bool ignoreMin, bool ignoreMax, bool? includeNull, bool reverse, ref Expression? result)
    {
        Expression minExp = null!;
        Expression maxExp = null!;

        if (!ignoreMin)
        {
            minExp = _compareFunctions[data.minData.filterType](
                data.minData.entityPropertyConvertTo != null ? Expression.Convert(Expression.Property(parameter, entityPropertyName), data.minData.entityPropertyConvertTo!) : Expression.Property(parameter, entityPropertyName),
                data.minData.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(min, typeof(TFilterMinProperty)), data.minData.filterPropertyConvertTo!) : Expression.Constant(min, typeof(TFilterMinProperty)));
        }

        if (!ignoreMax)
        {
            maxExp = _compareFunctions[data.maxData.filterType](
                data.maxData.entityPropertyConvertTo != null ? Expression.Convert(Expression.Property(parameter, entityPropertyName), data.maxData.entityPropertyConvertTo!) : Expression.Property(parameter, entityPropertyName),
                data.maxData.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(max, typeof(TFilterMaxProperty)), data.maxData.filterPropertyConvertTo!) : Expression.Constant(max, typeof(TFilterMaxProperty)));
        }

        Expression? exp;
        if (ignoreMin)
        {
            exp = ignoreMax ? null : maxExp;
        }
        else if (ignoreMax)
        {
            exp = minExp;
        }
        else
        {
            exp = Expression.AndAlso(minExp, maxExp);
        }

        if (exp != null)
        {
            if (reverse)
            {
                exp = Expression.Not(exp);
            }

            if (includeNull.HasValue)
            {
                exp = includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
            }
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildEntityRangeExpression<TEntityMinProperty, TEntityMaxProperty, TFilterProperty>(ParameterExpression parameter, string entityMinPropertyName, string entityMaxPropertyName, TFilterProperty value, RangeData data, bool? includeNullMin, bool? includeNullMax, bool reverse, ref Expression? result)
    {
        var filterPropertyType = typeof(TFilterProperty);
        Expression minExp = _compareFunctions[data.minData.filterType](
            data.minData.entityPropertyConvertTo != null ? Expression.Convert(Expression.Property(parameter, entityMinPropertyName), data.minData.entityPropertyConvertTo!) : Expression.Property(parameter, entityMinPropertyName),
            data.minData.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, filterPropertyType), data.minData.filterPropertyConvertTo!) : Expression.Constant(value, filterPropertyType));
        if (includeNullMin.HasValue)
        {
            minExp = includeNullMin.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityMinPropertyName), Expression.Constant(null, typeof(TEntityMinProperty))), minExp)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityMinPropertyName), Expression.Constant(null, typeof(TEntityMinProperty))), minExp);
        }

        Expression maxExp = _compareFunctions[data.maxData.filterType](
            data.maxData.entityPropertyConvertTo != null ? Expression.Convert(Expression.Property(parameter, entityMaxPropertyName), data.maxData.entityPropertyConvertTo!) : Expression.Property(parameter, entityMaxPropertyName),
            data.maxData.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, filterPropertyType), data.maxData.filterPropertyConvertTo!) : Expression.Constant(value, filterPropertyType));
        if (includeNullMax.HasValue)
        {
            maxExp = includeNullMax.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityMaxPropertyName), Expression.Constant(null, typeof(TEntityMaxProperty))), maxExp)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityMaxPropertyName), Expression.Constant(null, typeof(TEntityMaxProperty))), maxExp);
        }

        Expression exp = Expression.AndAlso(minExp, maxExp);
        if (reverse)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    private static FilterByPropertyType GetReversed(this FilterByPropertyType filterType)
    {
        return filterType switch
        {
            FilterByPropertyType.Contains => FilterByPropertyType.NotContains,
            FilterByPropertyType.Equality => FilterByPropertyType.InEquality,
            FilterByPropertyType.GreaterThan => FilterByPropertyType.LessThanOrEqual,
            FilterByPropertyType.GreaterThanOrEqual => FilterByPropertyType.LessThan,
            FilterByPropertyType.In => FilterByPropertyType.NotIn,
            FilterByPropertyType.InEquality => FilterByPropertyType.Equality,
            FilterByPropertyType.LessThan => FilterByPropertyType.GreaterThanOrEqual,
            FilterByPropertyType.LessThanOrEqual => FilterByPropertyType.GreaterThan,
            FilterByPropertyType.NotContains => FilterByPropertyType.Contains,
            _ => FilterByPropertyType.In,
        };
    }
}
