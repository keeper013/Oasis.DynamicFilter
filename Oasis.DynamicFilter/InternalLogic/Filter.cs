namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter;
using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

internal sealed class Filter : IFilter
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, SpecificFilter>> _expressionBuilderCache;

    public Filter(Dictionary<Type, Dictionary<Type, (MethodMetaData, IFilterPropertyExcludeByValueManager?)>> expressionBuilderCache, Type type)
    {
        var dict = new Dictionary<Type, IReadOnlyDictionary<Type, SpecificFilter>>();
        foreach (var kvp1 in expressionBuilderCache)
        {
            var innerDict = new Dictionary<Type, SpecificFilter>();
            foreach (var kvp2 in kvp1.Value)
            {
                innerDict.Add(kvp2.Key, new (Delegate.CreateDelegate(kvp2.Value.Item1.type, type.GetMethod(kvp2.Value.Item1.name, BindingFlags.Public | BindingFlags.Static)), kvp2.Value.Item2));
            }

            dict.Add(kvp1.Key, innerDict);
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
        var sf = _expressionBuilderCache.Find(entityType, filterType);
        return sf == null
            ? throw new FilterNotRegisteredException(entityType, filterType)
            : sf.Delegate is not Utilities.GetExpression<TFilter, TEntity> func
            ? throw new InvalidOperationException($"Invalid delegate for {filterType.Name} to {entityType.Name}.")
            : func(filter, sf.Manager);
    }

    private sealed class SpecificFilter
    {
        public SpecificFilter(Delegate del, IFilterPropertyExcludeByValueManager? manager)
        {
            Delegate = del;
            Manager = manager;
        }

        public Delegate Delegate { get; }

        public IFilterPropertyExcludeByValueManager? Manager { get; }
    }
}
