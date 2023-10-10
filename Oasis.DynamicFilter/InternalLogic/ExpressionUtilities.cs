namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, (Type?, Type?)>> _convertForComparisonDirectionDictionary = new Dictionary<Type, IReadOnlyDictionary<Type, (Type?, Type?)>>
    {
        {
            typeof(sbyte), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(short), (typeof(short), null) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (typeof(short), typeof(short)) },
                { typeof(ushort), (typeof(int), typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(byte), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(short), (typeof(short), null) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(sbyte), (typeof(short), typeof(short)) },
                { typeof(ushort), (typeof(ushort), null) },
                { typeof(uint), (typeof(uint), null) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(short), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(short)) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(short)) },
                { typeof(ushort), (typeof(int), typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(ushort), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(int), typeof(int)) },
                { typeof(short), (typeof(int), typeof(int)) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(ushort)) },
                { typeof(uint), (typeof(uint), null) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(int), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(int)) },
                { typeof(short), (null, typeof(int)) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(int)) },
                { typeof(ushort), (null, typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(uint), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(long), typeof(long)) },
                { typeof(short), (typeof(long), typeof(long)) },
                { typeof(int), (typeof(long), typeof(long)) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(uint)) },
                { typeof(ushort), (null, typeof(uint)) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(long), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(long)) },
                { typeof(short), (null, typeof(long)) },
                { typeof(int), (null, typeof(long)) },
                { typeof(byte), (null, typeof(long)) },
                { typeof(ushort), (null, typeof(long)) },
                { typeof(uint), (null, typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(ulong), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(byte), (null, typeof(ulong)) },
                { typeof(ushort), (null, typeof(ulong)) },
                { typeof(uint), (null, typeof(ulong)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(float), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(float)) },
                { typeof(short), (null, typeof(float)) },
                { typeof(int), (null, typeof(float)) },
                { typeof(long), (null, typeof(float)) },
                { typeof(byte), (null, typeof(float)) },
                { typeof(ushort), (null, typeof(float)) },
                { typeof(uint), (null, typeof(float)) },
                { typeof(ulong), (null, typeof(float)) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(double), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(double)) },
                { typeof(short), (null, typeof(double)) },
                { typeof(int), (null, typeof(double)) },
                { typeof(long), (null, typeof(double)) },
                { typeof(byte), (null, typeof(double)) },
                { typeof(ushort), (null, typeof(double)) },
                { typeof(uint), (null, typeof(double)) },
                { typeof(ulong), (null, typeof(double)) },
                { typeof(float), (null, typeof(double)) },
                { typeof(decimal), (typeof(decimal), null) },
            }
        },
        {
            typeof(decimal), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(decimal)) },
                { typeof(short), (null, typeof(decimal)) },
                { typeof(int), (null, typeof(decimal)) },
                { typeof(long), (null, typeof(decimal)) },
                { typeof(byte), (null, typeof(decimal)) },
                { typeof(ushort), (null, typeof(decimal)) },
                { typeof(uint), (null, typeof(decimal)) },
                { typeof(ulong), (null, typeof(decimal)) },
                { typeof(float), (null, typeof(decimal)) },
                { typeof(double), (null, typeof(decimal)) },
            }
        },
    };

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

        Expression exp = null!;
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

    internal static (Type?, Type?)? GetComparisonConversion(Type entityPropertyType, Type filterPropertyType)
    {
        if (entityPropertyType == filterPropertyType)
        {
            return (null, null);
        }
        else
        {
            return _convertForComparisonDirectionDictionary.TryGetValue(entityPropertyType, out var innerDict) && innerDict.TryGetValue(filterPropertyType, out var item) ? item : null;
        }
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
