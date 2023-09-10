namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public interface IConfigurator<TConfigurator>
    where TConfigurator : class
{
    TConfigurator Finish();
}

public interface IPropertyExcluder<TConfiguration>
    where TConfiguration : class
{
    TConfiguration ExcludeEntityProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class;

    TConfiguration ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition)
        where TFilter : class;

    TConfiguration ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option)
        where TFilter : class;
}

public interface IFilterBuilderConfiguration : IPropertyExcluder<IFilterBuilderConfiguration>, IConfigurator<IFilterBuilderFactory>
{
    IFilterBuilderConfiguration ExcludeEntityProperties(params string[] entityPropertyNames);

    IFilterBuilderConfiguration ExcludeEntityProperties(params Func<string, bool>[] conditions);
}

public interface IFilterBuilderFactory
{
    IFilterBuilderConfiguration Configure();

    IFilterBuilder Make();
}
