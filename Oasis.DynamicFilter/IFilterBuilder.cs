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
    Less = 0,

    /// <summary>
    /// Entity value is less than or equals filter.
    /// </summary>
    LessOrEqual = 1,
}

public interface IFilterConfigurationBuilder<TFilter, TEntity>
    where TFilter : class
    where TEntity : class
{
    IFilterConfigurationBuilder<TFilter, TEntity> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByPropertyType type,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Func<TFilter, bool>? ignoreIf = null,
        Func<TFilter, bool>? reverseIf = null);

    IFilterConfigurationBuilder<TFilter, TEntity> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMaxExpression,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null,
        Func<TFilter, bool>? reverseIf = null);

    IFilterConfigurationBuilder<TFilter, TEntity> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TFilter, TEntityProperty>> filterPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null,
        Func<TFilter, bool>? reverseIf = null);

    IFilterBuilder Finish();
}

public interface IFilterBuilder
{
    void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class;

    IFilterConfigurationBuilder<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class;

    IFilter Build();
}