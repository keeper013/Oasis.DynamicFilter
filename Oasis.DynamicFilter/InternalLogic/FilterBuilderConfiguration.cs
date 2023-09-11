namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using Oasis.DynamicFilter.Exceptions;
using System.Linq;

internal interface IGlobalPropertyExcluder
{
    bool IsPropertyExcluded(string propertyName);

    bool IsEntityPropertyExcluded(Type type, string propertyName);

    bool IsFilterPropertyAlwaysExcluded(Type type, string propertyName);
}

internal interface IFilterBuilderConfiguration : IGlobalPropertyExcluder
{
    ISet<string> ExcludedProperties { get; }

    IList<Func<string, bool>> ExcludedPropertyConditions { get; }

    Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; }

    Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; }
}

internal sealed class FilterBuilderConfiguration : IFilterBuilderConfigurationBuilder, IFilterBuilderConfiguration
{
    private readonly IFilterBuilderFactory _factory;

    public FilterBuilderConfiguration(IFilterBuilderFactory factory)
    {
        _factory = factory;
    }

    public ISet<string> ExcludedProperties { get; } = new HashSet<string>();

    public IList<Func<string, bool>> ExcludedPropertyConditions { get; } = new List<Func<string, bool>>();

    public Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; } = new ();

    public Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; } = new ();

    public Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; } = new ();

    public bool IsPropertyExcluded(string propertyName)
    {
        return ExcludedProperties.Contains(propertyName) || ExcludedPropertyConditions.Any(f => f(propertyName));
    }

    public bool IsEntityPropertyExcluded(Type type, string propertyName)
    {
        return IsPropertyExcluded(propertyName) || (ExcludedEntityProperties.TryGetValue(type, out var ts) && ts.Contains(propertyName));
    }

    public bool IsFilterPropertyAlwaysExcluded(Type type, string propertyName)
    {
        return ExcludedFilterPropertiesByOption.TryGetValue(type, out var inner) && inner.TryGetValue(propertyName, out var option) && option == ExcludingOption.Always;
    }

    public IFilterBuilderConfigurationBuilder ExcludeEntityProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TEntity);
        if (!ExcludedEntityProperties.AddIfNotExists(type, propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        return this;
    }

    public IFilterBuilderConfigurationBuilder ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition)
        where TFilter : class
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TFilter);
        if (ExcludedFilterPropertiesByOption.Contains(type, propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        if (!ExcludedFilterPropertiesByCondition.AddIfNotExists(type, propertyName, condition))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        return this;
    }

    public IFilterBuilderConfigurationBuilder ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option)
        where TFilter : class
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TFilter);
        if (ExcludedFilterPropertiesByCondition.Contains(type, propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        if (!ExcludedFilterPropertiesByOption.AddIfNotExists(type, propertyName, option))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        return this;
    }

    public IFilterBuilderConfigurationBuilder ExcludeEntityProperties(params string[] entityPropertyNames)
    {
        ExcludedProperties.Clear();
        foreach (var entityProperty in entityPropertyNames)
        {
            ExcludedProperties.Add(entityProperty);
        }

        return this;
    }

    public IFilterBuilderConfigurationBuilder ExcludeEntityProperties(params Func<string, bool>[] conditions)
    {
        ExcludedPropertyConditions.Clear();
        foreach (var condition in conditions)
        {
            ExcludedPropertyConditions.Add(condition);
        }

        return this;
    }

    public IFilterBuilderFactory Finish()
    {
        return _factory;
    }
}