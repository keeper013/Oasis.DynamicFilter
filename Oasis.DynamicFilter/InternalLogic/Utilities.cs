namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

internal record struct ContainerElementTypeData(Type elementType, bool isCollection);

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
    internal const string EqualityOperatorMethodName = "op_Equality";
    private const string NullableTypeName = "System.Nullable`1[[";
    private static readonly Type CollectionType = typeof(ICollection<>);

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

    internal static string GenerateRandomName(int length)
    {
        const string AvailableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int AvailableCharsCount = 52;
        var bytes = new byte[length];
        RandomNumberGenerator.Create().GetBytes(bytes);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }

    internal static bool HasEqualityOperator(this Type type) => type.GetMethod(EqualityOperatorMethodName, PublicStatic) != null;

    internal static void Add<TKey1, TKey2, TValue>(this IDictionary<TKey1, IDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value)
    {
        if (!dict.TryGetValue(key1, out var innerDict))
        {
            innerDict = new Dictionary<TKey2, TValue>();
            dict[key1] = innerDict;
        }

        innerDict.Add(key2, value);
    }

    internal static bool Remove<TKey1, TKey2, TValue>(this IDictionary<TKey1, IDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
    {
        var result = dict.TryGetValue(key1, out var innerDict) && innerDict.Remove(key2, out _);
        if (result)
        {
            if (innerDict.Count == 0)
            {
                dict.Remove(key1);
            }
        }

        return result;
    }

    internal static bool Contains<TKey1, TKey2, TValue>(this IDictionary<TKey1, IDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2) => dict.TryGetValue(key1, out var innerDict) && innerDict.ContainsKey(key2);

    internal static bool Contains<TKey1, TKey2>(this IReadOnlyDictionary<TKey1, ISet<TKey2>> dict, TKey1 key1, TKey2 key2) => dict.TryGetValue(key1, out var innserSet) && innserSet.Contains(key2);

    internal static TValue? Find<TKey1, TKey2, TValue>(this IDictionary<TKey1, IDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item)
            ? item : default;
    }

    private static bool IsOfGenericTypeDefinition(Type source, Type target) => source.IsGenericType && source.GetGenericTypeDefinition() == target;
}