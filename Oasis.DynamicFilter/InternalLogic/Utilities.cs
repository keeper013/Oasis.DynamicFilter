namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Oasis.DynamicFilter.Exceptions;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    public delegate Expression<Func<TEntity, bool>> GetExpression<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class;

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
}

internal record struct MethodMetaData(Type type, string name);