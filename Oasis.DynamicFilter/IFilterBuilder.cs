namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public enum FilteringType
{
    /// <summary>
    /// Entity value equals filter.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// Entity value is greater than filter.
    /// </summary>
    Greater = 1,

    /// <summary>
    /// Entity value is greater than or equals filter.
    /// </summary>
    GreaterOrEqual = 2,

    /// <summary>
    /// Entity value is less than filter.
    /// </summary>
    Less = 3,

    /// <summary>
    /// Entity value is less than or equals filter.
    /// </summary>
    LessOrEqual = 4,

    /// <summary>
    /// Entity value in filter range
    /// </summary>
    In = 5,

    /// <summary>
    /// Entity value not in filter range
    /// </summary>
    NotIn,
}

public enum ExcludingOption
{
    /// <summary>
    /// Ignoreing the property if it's value is default value
    /// </summary>
    DefaultValue,

    /// <summary>
    /// Never ignoring the property for filtering
    /// </summary>
    Never,
}

public interface IFilterConfiguration<TFilter, TEntity>
    where TFilter : class
    where TEntity : class
{
    IFilterConfiguration<TFilter, TEntity> Map(string filterPropertyName, string entityPropertyName);

    IFilterConfiguration<TFilter, TEntity> ExcludeEntityProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

    IFilterConfiguration<TFilter, TEntity> ExcludeFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool>? condition);

    IFilterConfiguration<TFilter, TEntity> ExcludingFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option);

    IFilterConfiguration<TFilter, TEntity> Configure(string filterPropertyName, string entityPropertyName, FilteringType filteringType);

    IFilterConfiguration<TFilter, TEntity> Configure(string propertyName, FilteringType filteringType);

    IFilterBuilder Finish();
}

public interface IFilterBuilder
{
    void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class;

    IFilterConfiguration<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class;

    IFilter Build();
}