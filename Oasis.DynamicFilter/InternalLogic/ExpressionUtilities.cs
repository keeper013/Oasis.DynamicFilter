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
            exp = includeNull.Value ^ reverse
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    public static void BuildCollectionContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, ContainData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        var collectionType = typeof(ICollection<TEntityPropertyItem>);
        Expression MakeContainsExpression()
        {
            var filterPropertyType = typeof(TFilterProperty);
            var containsMethod = collectionType!.GetMethod(nameof(ICollection<TEntityPropertyItem>.Contains))!;
            return Expression.Call(
                Expression.Property(parameter, entityPropertyName),
                containsMethod,
                data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
        }

        BuildContainsExpression<TEntityPropertyItem, TFilterProperty>(parameter, entityPropertyName, value, data, includeNull, reverse, collectionType, MakeContainsExpression, ref result);
    }

    public static void BuildArrayContainsExpression<TEntityPropertyItem, TFilterProperty>(ParameterExpression parameter, string entityPropertyName, TFilterProperty value, ContainData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        var arrayType = typeof(TEntityPropertyItem[]);
        Expression MakeContainsExpression()
        {
            var entityPropertyItemType = typeof(TEntityPropertyItem);
            var filterPropertyType = typeof(TFilterProperty);
            var containsMethod = EnumerableContains.MakeGenericMethod(entityPropertyItemType);
            return Expression.Call(
                containsMethod,
                Expression.Property(parameter, entityPropertyName),
                data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
        }

        BuildContainsExpression<TEntityPropertyItem, TFilterProperty>(parameter, entityPropertyName, value, data, includeNull, reverse, arrayType, MakeContainsExpression, ref result);
    }

    public static void BuildInCollectionExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, ICollection<TFilterPropertyItem> value, InData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        Expression MakeContainsExpression()
        {
            var collectionType = typeof(ICollection<TFilterPropertyItem>);
            var containsMethod = collectionType.GetMethod(nameof(ICollection<TFilterPropertyItem>.Contains))!;
            return Expression.Call(
                Expression.Constant(value),
                containsMethod,
                data.entityPropertyConvertTo == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.entityPropertyConvertTo));
        }

        BuildInExpression<TEntityProperty, TFilterPropertyItem>(
            parameter,
            entityPropertyName,
            value,
            data,
            includeNull,
            reverse,
            MakeContainsExpression,
            ref result);
    }

    public static void BuildInArrayExpression<TEntityProperty, TFilterPropertyItem>(ParameterExpression parameter, string entityPropertyName, TFilterPropertyItem[] value, InData data, bool? includeNull, bool reverse, ref Expression? result)
    {
        Expression MakeContainsExpression()
        {
            var filterPropertyItemType = typeof(TFilterPropertyItem);
            var containsMethod = EnumerableContains.MakeGenericMethod(filterPropertyItemType);
            return Expression.AndAlso(
                Expression.NotEqual(Expression.Constant(value), Expression.Constant(null, typeof(TFilterPropertyItem[]))),
                Expression.Call(
                    containsMethod,
                    Expression.Constant(value),
                    data.entityPropertyConvertTo == null ? Expression.Property(parameter, entityPropertyName) : Expression.Convert(Expression.Property(parameter, entityPropertyName), data.entityPropertyConvertTo)));
        }

        BuildInExpression<TEntityProperty, TFilterPropertyItem>(
            parameter,
            entityPropertyName,
            value,
            data,
            includeNull,
            reverse,
            MakeContainsExpression,
            ref result);
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
            if (includeNull.HasValue)
            {
                exp = includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), exp);
            }

            if (reverse)
            {
                exp = Expression.Not(exp);
            }

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
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

    private static void BuildContainsExpression<TEntityPropertyItem, TFilterProperty>(
        ParameterExpression parameter,
        string entityPropertyName,
        TFilterProperty value,
        ContainData data,
        bool? includeNull,
        bool reverse,
        Type containerType,
        Func<Expression> makeContainsExpression,
        ref Expression? result)
    {
        Expression exp;
        var notContains = data.filterType == FilterByPropertyType.NotContains;

        // can't call contains, if entity property isn't null then not contains
        if (data.nullValueNotCovered && value == null)
        {
            exp = includeNull.HasValue && includeNull.Value
                ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, containerType)), Expression.Constant(notContains))
                : Expression.Constant(notContains);
        }
        else
        {
            var containsExpression = makeContainsExpression();
            if (notContains)
            {
                containsExpression = Expression.Not(containsExpression);
            }

            exp = includeNull.HasValue
                ? includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, containerType)), containsExpression)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, containerType)), containsExpression)
                : Expression.Condition(
                    Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, containerType)),
                    Expression.Constant(notContains),
                    containsExpression);
        }

        if (reverse)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    private static void BuildInExpression<TEntityProperty, TFilterPropertyItem>(
        ParameterExpression parameter,
        string entityPropertyName,
        IEnumerable<TFilterPropertyItem> value,
        InData data,
        bool? includeNull,
        bool reverse,
        Func<Expression> makeContainsExpression,
        ref Expression? result)
    {
        var notIn = data.filterType == FilterByPropertyType.NotIn;
        Expression exp;
        if (value == null)
        {
            exp = includeNull.HasValue
                ? includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), Expression.Constant(notIn))
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), Expression.Constant(notIn))
                : Expression.Constant(notIn);
        }
        else
        {
            Expression containsExpression = makeContainsExpression();
            if (notIn)
            {
                containsExpression = Expression.Not(containsExpression);
            }

            if (data.nullValueNotCovered)
            {
                exp = Expression.Condition(
                    Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))),
                    Expression.Constant(includeNull.HasValue ? includeNull.Value : notIn),
                    containsExpression);
            }
            else if (includeNull.HasValue)
            {
                exp = includeNull.Value
                    ? Expression.OrElse(Expression.Equal(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), containsExpression)
                    : Expression.AndAlso(Expression.NotEqual(Expression.Property(parameter, entityPropertyName), Expression.Constant(null, typeof(TEntityProperty))), containsExpression);
            }
            else
            {
                exp = containsExpression;
            }
        }

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
