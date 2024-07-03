namespace Oasis.DynamicFilter;

using System.Linq.Expressions;
using System;

public enum Operator
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

public enum RangeOperator
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

public enum StringOperator
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
    /// Entity value contains filter value
    /// </summary>
    Contains = 2,

    /// <summary>
    /// Entity value not contains filter value
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// Filter value contains entity value
    /// </summary>
    In = 4,

    /// <summary>
    /// Filter value not contains entity value
    /// </summary>
    NotIn = 5,

    /// <summary>
    /// Entity value contains filter value
    /// </summary>
    StartsWith = 6,

    /// <summary>
    /// Entity value not contains filter value
    /// </summary>
    NotStartsWith = 7,

    /// <summary>
    /// Entity value contains filter value
    /// </summary>
    EndsWith = 8,

    /// <summary>
    /// Entity value not contains filter value
    /// </summary>
    NotEndsWith = 9,
}

public interface IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    IFilterConfigurationBuilder<TEntity, TFilter> ExcludeProperties<TEntityProperty>(params Expression<Func<TEntity, TEntityProperty>>[] entityPropertyExpressions);

    IFilterConfigurationBuilder<TEntity, TFilter> Filter(Func<TFilter, Expression<Func<TEntity, bool>>> filterMethod);

    IFilterBuilder Finish();
}

public interface IFilterBuilder
{
    IFilterBuilder Register<TEntity, TFilter>(StringOperator? defaultStringOperator = null)
        where TEntity : class
        where TFilter : class;

    IFilterConfigurationBuilder<TEntity, TFilter> Configure<TEntity, TFilter>(StringOperator? defaultStringOperator = null)
        where TEntity : class
        where TFilter : class;

    IFilter Build();
}