namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using Oasis.DynamicFilter.Exceptions;
using System.Linq;

internal interface IGlobalFilterPropertyExcludeByValueManager
{
    bool IsFilterPropertyExcluded<TProperty>(Type filterType, string propertyName, TProperty value);
}

internal sealed class GlobalFilterPropertyExcludeByValueManager : IGlobalFilterPropertyExcludeByValueManager
{
    private readonly IReadOnlyDictionary<Type, Delegate> _equalityManager;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Delegate>>? _byCondition;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>? _byOption;

    public GlobalFilterPropertyExcludeByValueManager(
        IReadOnlyDictionary<Type, Delegate> equalityManager,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Delegate>>? byCondition,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>? byOption)
    {
        _equalityManager = equalityManager;
        _byCondition = byCondition;
        _byOption = byOption;
    }

    public bool IsFilterPropertyExcluded<TProperty>(Type filterType, string propertyName, TProperty value)
    {
        return (_byOption != null && _byOption.TryGetValue(filterType, out var po) && po.TryGetValue(propertyName, out var o) && (o == ExcludingOption.Always || (o == ExcludingOption.DefaultValue && (_equalityManager[typeof(TProperty)] as Func<TProperty?, TProperty?, bool>)!(value, default))))
            || (_byCondition != null && (_byCondition.Find(filterType, propertyName) as Func<TProperty, bool>)!(value));
    }
}

internal interface IGlobalExcludedPropertyManager
{
    IGlobalFilterPropertyExcludeByValueManager? FilterPropertyExcludeByValueManager { get; }

    bool IsPropertyExcluded(string propertyName);

    bool IsEntityPropertyExcluded(Type entityType, string propertyName);

    bool IsFilterPropertyExcluded(Type filterType, string propertyName);
}

internal interface IFilterBuilderConfiguration : IGlobalExcludedPropertyManager
{
    ISet<string> ExcludedProperties { get; }

    IList<Func<string, bool>> ExcludedPropertyConditions { get; }

    Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; }

    Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; }
}

internal sealed class FilterBuilderConfiguration : IFilterBuilderConfigurationBuilder, IFilterBuilderConfiguration
{
    private readonly FilterBuilderBuilder _builder;
    private readonly IEqualityMethodBuilder _equalityMethodBuilder;
    private IGlobalFilterPropertyExcludeByValueManager? _globalFilterPropertyExcludeByValueManager;

    public FilterBuilderConfiguration(FilterBuilderBuilder builder, IEqualityMethodBuilder equalityMethodBuilder)
    {
        _builder = builder;
        _equalityMethodBuilder = equalityMethodBuilder;
    }

    public ISet<string> ExcludedProperties { get; } = new HashSet<string>();

    public IList<Func<string, bool>> ExcludedPropertyConditions { get; } = new List<Func<string, bool>>();

    public Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; } = new ();

    public Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; } = new ();

    public Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; } = new ();

    public IGlobalFilterPropertyExcludeByValueManager? FilterPropertyExcludeByValueManager => _globalFilterPropertyExcludeByValueManager;

    internal Dictionary<Type, MethodMetaData> PropertyTypeEqualityDict { get; } = new ();

    public bool IsPropertyExcluded(string propertyName)
    {
        return ExcludedProperties.Contains(propertyName) || ExcludedPropertyConditions.Any(f => f(propertyName));
    }

    public bool IsEntityPropertyExcluded(Type entityType, string propertyName)
    {
        return IsPropertyExcluded(propertyName) || (ExcludedEntityProperties.TryGetValue(entityType, out var ts) && ts.Contains(propertyName));
    }

    public bool IsFilterPropertyExcluded(Type filterType, string propertyName)
    {
        return ExcludedFilterPropertiesByOption.TryGetValue(filterType, out var inner) && inner.TryGetValue(propertyName, out var option) && option == ExcludingOption.Always;
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

        var propertyType = typeof(TProperty);
        if (option == ExcludingOption.DefaultValue && !PropertyTypeEqualityDict.ContainsKey(propertyType))
        {
            PropertyTypeEqualityDict.Add(propertyType, _equalityMethodBuilder.BuildEqualityMethod(propertyType));
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

    public IFilterBuilderBuilder Finish()
    {
        Dictionary<Type, IReadOnlyDictionary<string, Delegate>>? byCondition = null;
        if (ExcludedFilterPropertiesByCondition.Any())
        {
            byCondition = new Dictionary<Type, IReadOnlyDictionary<string, Delegate>>();
            foreach (var kvp1 in ExcludedFilterPropertiesByCondition)
            {
                byCondition[kvp1.Key] = kvp1.Value;
            }
        }

        Dictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>? byOption = null;
        if (ExcludedFilterPropertiesByOption.Any())
        {
            byOption = new Dictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>();
            foreach (var kvp2 in ExcludedFilterPropertiesByOption)
            {
                byOption[kvp2.Key] = kvp2.Value;
            }
        }

        if (byCondition != null || byOption != null)
        {
            _globalFilterPropertyExcludeByValueManager = new GlobalFilterPropertyExcludeByValueManager(_builder.EqualityManager, byCondition, byOption);
        }

        return _builder;
    }
}