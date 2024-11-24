namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter;
using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;

internal sealed class Filter : IFilter
{
    private readonly IDictionary<Type, IDictionary<Type, Delegate>> _filterBuilders;
    private readonly ModuleBuilder? _moduleBuilder;
    private IDictionary<Type, IDictionary<Type, ICanConvertToDelegate>>? _lazyFilterBuilders;
    private bool _needThreadSafty;

    public Filter(
        IDictionary<Type, IDictionary<Type, Delegate>> filterBuilders,
        IDictionary<Type, IDictionary<Type, ICanConvertToDelegate>>? lazyFilterBuilders,
        ModuleBuilder? moduleBuilder)
    {
        _lazyFilterBuilders = lazyFilterBuilders;
        _filterBuilders = filterBuilders;
        _moduleBuilder = moduleBuilder;
        _needThreadSafty = _moduleBuilder is not null || lazyFilterBuilders != null;
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
        Delegate? del;
        if (_needThreadSafty)
        {
            lock (this)
            {
                del = _filterBuilders.Find(entityType, filterType);
                if (del == null && _lazyFilterBuilders != null)
                {
                    var configuration = _lazyFilterBuilders.Find(entityType, filterType);
                    if (configuration != null)
                    {
                        del = configuration.ToDelegate();
                        _filterBuilders.Add(entityType, filterType, del);
                        _lazyFilterBuilders.Remove(entityType, filterType);
                        if (_lazyFilterBuilders.Count == 0)
                        {
                            _lazyFilterBuilders = null;
                            _needThreadSafty = _moduleBuilder is not null;
                        }
                    }
                }

                if (del == null && _moduleBuilder != null)
                {
                    del = FilterBuilder.BuildDelegate<TEntity, TFilter>(_moduleBuilder);
                    _filterBuilders.Add(entityType, filterType, del);
                }
            }
        }
        else
        {
            del = _filterBuilders.Find(entityType, filterType);
        }

        if (del == null)
        {
            throw new FilterNotRegisteredException(entityType, filterType);
        }

        return (del as Func<TFilter, Expression<Func<TEntity, bool>>>)!(filter);
    }
}
