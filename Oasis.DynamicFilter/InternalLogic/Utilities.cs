namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Reflection;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    public delegate Expression<Func<TEntity, bool>> GetExpression<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class;

    internal static void AddIfNotExists<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, T value, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(sourceType, out var innerDict))
            {
                innerDict = new Dictionary<Type, T>();
                dict[sourceType] = innerDict;
            }

            if (!innerDict.ContainsKey(targetType))
            {
                innerDict.Add(targetType, value);
            }
        }
    }

    internal static void AddIfNotExists<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, Func<T> func, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(sourceType, out var innerDict))
            {
                innerDict = new Dictionary<Type, T>();
                dict[sourceType] = innerDict;
            }

            if (!innerDict.ContainsKey(targetType))
            {
                innerDict![targetType] = func();
            }
        }
    }

    internal static T? Find<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDict) && innerDict.TryGetValue(targetType, out var item)
            ? item : default;
    }

    internal static T? Find<T>(this IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDict) && innerDict.TryGetValue(targetType, out var item)
            ? item : default;
    }

    internal static bool Contains<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDict) && innerDict.ContainsKey(targetType);
    }
}

internal record struct MethodMetaData(Type type, string name);