namespace Oasis.DynamicFilter;

using System.Linq.Expressions;
using System;

public enum FilterByPropertyType
{
    /// <summary>
    /// Entity value equals filter value
    /// </summary>
    Equality = 0,

    /// <summary>
    /// Entity value not equals filter value
    /// </summary>
    InEquality = 1,

    /// <summary>
    /// Entity value is greater than filter.
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// Entity value is greater than or equals filter.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// Entity value is less than filter.
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// Entity value is less than or equals filter.
    /// </summary>
    LessThanOrEqual = 5,

    /// <summary>
    /// Filter collection contains entity value.
    /// </summary>
    Contains = 6,

    /// <summary>
    /// Filter collection not contains entity value.
    /// </summary>
    NotContains = 7,

    /// <summary>
    /// Filter value in entity collection.
    /// </summary>
    In = 8,

    /// <summary>
    /// Filter value not in entity collection
    /// </summary>
    NotIn = 9,
}

public enum FilterByRangeType
{
    /// <summary>
    /// Entity value is less than filter.
    /// </summary>
    LessThan = 0,

    /// <summary>
    /// Entity value is less than or equals filter.
    /// </summary>
    LessThanOrEqual = 1,
}

public interface IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    IFilterConfigurationBuilder<TEntity, TFilter> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByPropertyType type,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null);

    IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TEntityProperty, TMinFilterProperty, TMaxFilterProperty>(
        Expression<Func<TFilter, TMinFilterProperty>> filterPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TFilter, TMaxFilterProperty>> filterPropertyMaxExpression,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null);

    IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TMinEntityProperty, TFilterProperty, TMaxEntityProperty>(
        Expression<Func<TEntity, TMinEntityProperty>> entityPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TEntity, TMaxEntityProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null);

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