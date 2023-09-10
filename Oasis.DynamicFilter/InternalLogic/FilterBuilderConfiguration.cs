namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using Oasis.DynamicFilter.Exceptions;

internal interface IExcludedProperties
{
    Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; }

    Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; }
}

internal interface IFilterGlobalConfiguration : IExcludedProperties
{
    ISet<string> ExcludedProperties { get; }

    IList<Func<string, bool>> ExcludedPropertyConditions { get; }
}

internal abstract class PropertyExcluder<TConfiguration> : IPropertyExcluder<TConfiguration>, IExcludedProperties
    where TConfiguration : class
{
    public Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; } = new ();

    public Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; } = new ();

    public Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; } = new ();

    protected TConfiguration Configuration { private get; set; } = null!;

    public TConfiguration ExcludeEntityProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TEntity);
        if (!ExcludedEntityProperties.AddIfNotExists(type, propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        return Configuration;
    }

    public TConfiguration ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition)
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

        return Configuration;
    }

    public TConfiguration ExcludeFilterProperty<TFilter, TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option)
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

        return Configuration;
    }
}

internal sealed class FilterBuilderConfiguration : PropertyExcluder<IFilterBuilderConfiguration>, IFilterBuilderConfiguration, IFilterGlobalConfiguration
{
    private readonly IFilterBuilderFactory _factory;

    public FilterBuilderConfiguration(IFilterBuilderFactory factory)
    {
        _factory = factory;
        Configuration = this;
    }

    public ISet<string> ExcludedProperties { get; } = new HashSet<string>();

    public IList<Func<string, bool>> ExcludedPropertyConditions { get; } = new List<Func<string, bool>>();

    public IFilterBuilderConfiguration ExcludeEntityProperties(params string[] entityPropertyNames)
    {
        ExcludedProperties.Clear();
        foreach (var entityProperty in entityPropertyNames)
        {
            ExcludedProperties.Add(entityProperty);
        }

        return this;
    }

    public IFilterBuilderConfiguration ExcludeEntityProperties(params Func<string, bool>[] conditions)
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