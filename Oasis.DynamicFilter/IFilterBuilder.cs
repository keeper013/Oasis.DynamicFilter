namespace Oasis.DynamicFilter;

using System.Linq.Expressions;
using System;

public enum FilteringType
{
    /// <summary>
    /// Entity value equals filter value for scalar, in filter values for collection.
    /// </summary>
    Default = 0,

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
    /// Entity value not in filter range
    /// </summary>
    NotIn = 5,
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

    /// <summary>
    /// Alaways ingoring the property for filtering
    /// </summary>
    Always,
}

public interface IFilterConfigurationBuilder<TFilter, TEntity> : IConfigurator<IFilterBuilder>
    where TFilter : class
    where TEntity : class
{
    IFilterConfigurationBuilder<TFilter, TEntity> ExcludeEntityProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

    IFilterConfigurationBuilder<TFilter, TEntity> ExcludeFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition);

    IFilterConfigurationBuilder<TFilter, TEntity> ExcludeFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option);

    IFilterConfigurationBuilder<TFilter, TEntity> Configure<TFilterProperty, TEntityProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilteringType filteringType = FilteringType.Default);
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