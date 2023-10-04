namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Oasis.DynamicFilter;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
    internal const string EqualityOperatorMethodName = "op_Equality";
    internal const string InequalityOperatorMethodName = "op_Inequality";
    internal const string GreaterThanOperatorMethodName = "op_GreaterThan";
    internal const string GreaterThanOrEqualOperatorMethodName = "op_GreaterThanOrEqual";
    internal const string LessThanOperatorMethodName = "op_LessThan";
    internal const string LessThanOrEqualOperatorMethodName = "op_LessThanOrEqual";
    private const string NullableTypeName = "System.Nullable`1[[";
    private static readonly Type CollectionType = typeof(ICollection<>);
    private static readonly Type[] NonPrimitiveScalarTypes = new[]
    {
        typeof(string), typeof(decimal), typeof(decimal?), typeof(DateTime), typeof(DateTime?),
    };

    public static bool IsFilterableType(this Type type)
        => type.IsPrimitive || type.IsEnum || type.GetMethod(EqualityOperatorMethodName, PublicStatic) != default
            || (type.IsNullable(out var argumentType) && (argumentType.IsPrimitive || argumentType.IsEnum || argumentType.GetMethod(EqualityOperatorMethodName, PublicStatic) != default))
            || NonPrimitiveScalarTypes.Contains(type);

    public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? argumentType)
    {
        if (type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1)
        {
            argumentType = type.GenericTypeArguments[0];
            return true;
        }

        argumentType = null;
        return false;
    }

    public static bool IsStruct(this Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
    }

    public static bool Matches(this Type type1, Type type2)
    {
        if (type1 == type2)
        {
            return true;
        }

        if (type1.FullName!.StartsWith(NullableTypeName) && type1.GenericTypeArguments.Length == 1)
        {
            type1 = type1.GenericTypeArguments[0];
        }

        if (type2.FullName!.StartsWith(NullableTypeName) && type2.GenericTypeArguments.Length == 1)
        {
            type2 = type2.GenericTypeArguments[0];
        }

        return type1 == type2;
    }

    public static Type? GetListItemType(this Type type)
    {
        var listType = type.GetListType();
        if (listType != default)
        {
            return listType.GenericTypeArguments[0];
        }

        return default;
    }

    public static Type? GetListType(this Type type)
    {
        if (type.IsArray)
        {
            return default;
        }

        if (IsOfGenericTypeDefinition(type, CollectionType))
        {
            return type;
        }

        var types = type.GetInterfaces().Where(i => IsOfGenericTypeDefinition(i, CollectionType)).ToList();
        return types.Count == 1 ? types[0] : default;
    }

    internal static bool HasOperator(this Type type, FilterByPropertyType filterType)
    {
        string methodName = null!;
        switch (filterType)
        {
            case FilterByPropertyType.Equality:
                methodName = EqualityOperatorMethodName;
                break;
            case FilterByPropertyType.GreaterThan:
                methodName = GreaterThanOperatorMethodName;
                break;
            case FilterByPropertyType.GreaterThanOrEqual:
                methodName = GreaterThanOrEqualOperatorMethodName;
                break;
            case FilterByPropertyType.LessThan:
                methodName = LessThanOperatorMethodName;
                break;
            case FilterByPropertyType.LessThanOrEqual:
                methodName = LessThanOrEqualOperatorMethodName;
                break;
            default:
                methodName = InequalityOperatorMethodName;
                break;
        }

        return type.GetMethod(methodName, PublicStatic) != null;
    }

    internal static bool AddIfNotExists<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dict, TKey key, TValue value)
    {
        if (dict.TryGetValue(key, out var st))
        {
            return st.Add(value);
        }
        else
        {
            dict.Add(key, new HashSet<TValue> { value });
            return true;
        }
    }

    internal static bool AddIfNotExists<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(key1, out var innerDict))
            {
                innerDict = new Dictionary<TKey2, TValue>();
                dict[key1] = innerDict;
            }

            if (!innerDict.ContainsKey(key2))
            {
                innerDict.Add(key2, value);
                return true;
            }
        }

        return false;
    }

    internal static bool AddIfNotExists<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, Func<TValue> func, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(key1, out var innerDict))
            {
                innerDict = new Dictionary<TKey2, TValue>();
                dict[key1] = innerDict;
            }

            if (!innerDict.ContainsKey(key2))
            {
                innerDict![key2] = func();
                return true;
            }
        }

        return false;
    }

    internal static bool Contains<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.ContainsKey(key2);
    }

    internal static TValue? Find<TKey1, TKey2, TValue>(this IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item)
            ? item : default;
    }

    internal static PropertyInfo GetProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        MemberExpression? memberExpression = null;
        if (expression.Body.NodeType == ExpressionType.Convert)
        {
            var body = (UnaryExpression)expression.Body;
            memberExpression = body.Operand as MemberExpression;
        }
        else if (expression.Body.NodeType == ExpressionType.MemberAccess)
        {
            memberExpression = expression.Body as MemberExpression;
        }

        if (memberExpression == null)
        {
            throw new ArgumentException("Not a member access", nameof(expression));
        }

        var member = memberExpression.Member;
        var property = member as PropertyInfo;
        return property == null
            ? throw new InvalidOperationException(string.Format("Member with Name '{0}' is not a property.", member.Name))
            : property;
    }

    private static bool IsOfGenericTypeDefinition(Type source, Type target) => source.IsGenericType && source.GetGenericTypeDefinition() == target;
}

internal record struct MethodMetaData(Type type, string name);