namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Oasis.DynamicFilter;
using System.Security.Cryptography;

internal record struct ContainerElementTypeData(Type elementType, bool isCollection);

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

    public static bool IsScalarType(this Type type) => type.IsNullable(out _) || type.IsValueType || type == typeof(string);

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

    public static ContainerElementTypeData? GetContainerElementType(this Type type)
    {
        if (type.IsArray)
        {
            return new (type.GetElementType(), false);
        }

        if (IsOfGenericTypeDefinition(type, CollectionType))
        {
            return new (type.GenericTypeArguments[0], true);
        }

        var types = type.GetInterfaces().Where(i => IsOfGenericTypeDefinition(i, CollectionType)).ToList();
        return types.Count == 1 ? new (types[0].GenericTypeArguments[0], true) : null;
    }

    internal static string GenerateRandomTypeName(int length)
    {
        const string AvailableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int AvailableCharsCount = 52;
        var bytes = new byte[length];
        RandomNumberGenerator.Create().GetBytes(bytes);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }

    internal static bool HasOperator(this Type type, FilterBy filterType)
    {
        string methodName = null!;
        switch (filterType)
        {
            case FilterBy.Equality:
                methodName = EqualityOperatorMethodName;
                break;
            case FilterBy.GreaterThan:
                methodName = GreaterThanOperatorMethodName;
                break;
            case FilterBy.GreaterThanOrEqual:
                methodName = GreaterThanOrEqualOperatorMethodName;
                break;
            case FilterBy.LessThan:
                methodName = LessThanOperatorMethodName;
                break;
            case FilterBy.LessThanOrEqual:
                methodName = LessThanOrEqualOperatorMethodName;
                break;
            default:
                methodName = InequalityOperatorMethodName;
                break;
        }

        return type.GetMethod(methodName, PublicStatic) != null;
    }

    internal static void Add<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value)
    {
        if (!dict.TryGetValue(key1, out var innerDict))
        {
            innerDict = new Dictionary<TKey2, TValue>();
            dict[key1] = innerDict;
        }

        innerDict.Add(key2, value);
    }

    internal static void Add<TKey1, TKey2, TKey3, TValue>(this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>> dict, TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
    {
        if (!dict.TryGetValue(key1, out var innerDict))
        {
            innerDict = new Dictionary<TKey2, Dictionary<TKey3, TValue>>();
            dict[key1] = innerDict;
        }

        if (!innerDict.TryGetValue(key2, out var innerInnerDict))
        {
            innerInnerDict = new Dictionary<TKey3, TValue>();
            innerDict.Add(key2, innerInnerDict);
        }

        innerInnerDict.Add(key3, value);
    }

    internal static bool Contains<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2) => dict.TryGetValue(key1, out var innerDict) && innerDict.ContainsKey(key2);

    internal static bool Contains<TKey1, TKey2>(this IReadOnlyDictionary<TKey1, ISet<TKey2>> dict, TKey1 key1, TKey2 key2) => dict.TryGetValue(key1, out var innserSet) && innserSet.Contains(key2);

    internal static bool Contains<TKey1, TKey2, TKey3, TValue>(this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>> dict, TKey1 key1, TKey2 key2, TKey3 key3)
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var innerInnerDict) && innerInnerDict.ContainsKey(key3);
    }

    internal static TValue? Find<TKey1, TKey2, TValue>(this IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item)
            ? item : default;
    }

    private static bool IsOfGenericTypeDefinition(Type source, Type target) => source.IsGenericType && source.GetGenericTypeDefinition() == target;
}

internal record struct MethodMetaData(Type type, string name);