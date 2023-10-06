namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter;
using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

internal sealed class Filter : IFilter
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _expressionBuilderCache;

    public Filter(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> expressionBuilderCache)
    {
        _expressionBuilderCache = expressionBuilderCache;
    }

    public Expression<Func<TEntity, bool>> GetExpression<TEntity, TFilter>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        return GetExpressionInternal<TEntity, TFilter>(filter);
    }

    public Func<TEntity, bool> GetFunc<TEntity, TFilter>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        return GetExpressionInternal<TEntity, TFilter>(filter).Compile();
    }

    private Expression<Func<TEntity, bool>> GetExpressionInternal<TEntity, TFilter>(TFilter filter)
        where TFilter : class
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        var sf = _expressionBuilderCache.Find(entityType, filterType);
        return sf == null
            ? throw new FilterNotRegisteredException(entityType, filterType)
            : sf is not Func<TFilter, Expression<Func<TEntity, bool>>> func
            ? throw new InvalidOperationException($"Invalid delegate for {filterType.Name} to {entityType.Name}.")
            : func(filter);
    }
}
