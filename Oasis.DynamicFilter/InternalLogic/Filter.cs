namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

internal sealed class Filter : IFilter
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _expressionBuilderCache;

    public Filter(Dictionary<Type, Dictionary<Type, MethodMetaData>> expressionBuilderCache, Type type)
    {
        var dict = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var kvp in expressionBuilderCache)
        {
            var innerDict = new Dictionary<Type, Delegate>();
            foreach (var kvp2 in kvp.Value)
            {
                innerDict.Add(kvp2.Key, Delegate.CreateDelegate(kvp2.Value.type, type.GetMethod(kvp2.Value.name, BindingFlags.Public | BindingFlags.Static)));
            }

            dict.Add(kvp.Key, innerDict);
        }

        _expressionBuilderCache = dict;
    }

    public Expression<Func<TEntity, bool>> GetExpression<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        return GetExpressionInternal<TFilter, TEntity>(filter);
    }

    public Func<TEntity, bool> GetFunc<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        return GetExpressionInternal<TFilter, TEntity>(filter).Compile();
    }

    private Expression<Func<TEntity, bool>> GetExpressionInternal<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        var dgt = _expressionBuilderCache.Find(entityType, filterType);
        return dgt == null
            ? throw new FilterNotRegisteredException(entityType, filterType)
            : dgt is not Utilities.GetExpression<TFilter, TEntity> func
            ? throw new InvalidOperationException($"Invalid delegate for {filterType.Name} to {entityType.Name}.")
            : func(filter);
    }
}
