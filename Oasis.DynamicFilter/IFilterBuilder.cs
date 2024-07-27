namespace Oasis.DynamicFilter;

using System.Linq.Expressions;
using System;

public interface IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    IFilterConfigurationBuilder<TEntity, TFilter> ExcludeProperties<TEntityProperty>(params Expression<Func<TEntity, TEntityProperty>>[] entityPropertyExpressions);

    IFilterConfigurationBuilder<TEntity, TFilter> Filter(Func<TFilter, Expression<Func<TEntity, bool>>> filterMethod, Func<TFilter, bool>? applyFilter = null);

    IFilterBuilder Finish();
}

public interface IFilterBuilder
{
    IFilterBuilder Register<TEntity, TFilter>()
        where TEntity : class
        where TFilter : class;

    IFilterConfigurationBuilder<TEntity, TFilter> Configure<TEntity, TFilter>()
        where TEntity : class
        where TFilter : class;

    IFilter Build();
}