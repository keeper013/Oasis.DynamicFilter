namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public interface IConfigurator<TConfigurator>
    where TConfigurator : class
{
    TConfigurator Finish();
}

public interface IFilterBuilderConfigurationBuilder : IConfigurator<IFilterBuilderFactory>
{
    IFilterBuilderConfigurationBuilder ExcludeEntityProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class;

    IFilterBuilderConfigurationBuilder ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition)
        where TFilter : class;

    IFilterBuilderConfigurationBuilder ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option)
        where TFilter : class;

    IFilterBuilderConfigurationBuilder ExcludeEntityProperties(params string[] entityPropertyNames);

    IFilterBuilderConfigurationBuilder ExcludeEntityProperties(params Func<string, bool>[] conditions);
}

public interface IFilterBuilderFactory
{
    IFilterBuilder Make();
}
