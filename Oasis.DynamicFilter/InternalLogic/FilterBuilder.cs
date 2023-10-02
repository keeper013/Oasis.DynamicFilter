namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

public interface IFilterPropertyExcludeByValueManager
{
    bool IsFilterPropertyExcluded<TProperty>(string propertyName, TProperty value);
}

internal sealed class FilterPropertyExcludeByValueManager<TFilter> : IFilterPropertyExcludeByValueManager
    where TFilter : class
{
    private readonly IReadOnlyDictionary<Type, Delegate> _equalityManager;
    private readonly IGlobalFilterPropertyExcludeByValueManager? _globalFilterPropertyExcludeByValueManager;
    private readonly IReadOnlyDictionary<string, Delegate>? _byCondition;
    private readonly IReadOnlyDictionary<string, ExcludingOption>? _byOption;

    public FilterPropertyExcludeByValueManager(IReadOnlyDictionary<Type, Delegate> equalityManager, IGlobalFilterPropertyExcludeByValueManager globalFilterPropertyExcludeByValueManager)
    {
        _equalityManager = equalityManager;
        _globalFilterPropertyExcludeByValueManager = globalFilterPropertyExcludeByValueManager;
        _byCondition = null;
        _byOption = null;
    }

    public FilterPropertyExcludeByValueManager(
        IReadOnlyDictionary<Type, Delegate> equalityManager,
        IGlobalFilterPropertyExcludeByValueManager? globalFilterPropertyExcludeByValueManager,
        IReadOnlyDictionary<string, Delegate>? byCondition,
        IReadOnlyDictionary<string, ExcludingOption>? byOption)
    {
        _equalityManager = equalityManager;
        _globalFilterPropertyExcludeByValueManager = globalFilterPropertyExcludeByValueManager;
        _byCondition = byCondition;
        _byOption = byOption;
    }

    public bool IsFilterPropertyExcluded<TProperty>(string propertyName, TProperty value)
    {
        return (_globalFilterPropertyExcludeByValueManager != null && _globalFilterPropertyExcludeByValueManager.IsFilterPropertyExcluded(typeof(TFilter), propertyName, value))
            || (_byOption != null && _byOption.TryGetValue(propertyName, out var o) && (o == ExcludingOption.Always || (o == ExcludingOption.DefaultValue && (_equalityManager[typeof(TProperty)] as Func<TProperty?, TProperty?, bool>)!(value, default))))
            || (_byCondition != null && _byCondition.TryGetValue(propertyName, out var c) && (c as Func<TProperty, bool>)!(value));
    }
}

internal interface IFilterPropertyManager
{
    bool IsEntityPropertyExcluded(string propertyName);

    bool IsFilterPropertyExcluded(string propertyName);

    (string, FilteringType) GetFilteringType(string propertyName);
}

internal interface IFilterConfiguration : IFilterPropertyManager
{
    HashSet<string> ExcludedEntityProperties { get; }

    Dictionary<string, Delegate> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<string, ExcludingOption> ExcludedFilterPropertiesByOption { get; }

    public Dictionary<string, (string, FilteringType)> FilterDictionary { get; }

    IFilterPropertyExcludeByValueManager? GetFilterPropertyExcludeByValueManager();
}

internal sealed class FilterConfiguration<TFilter, TEntity> : IFilterConfigurationBuilder<TFilter, TEntity>, IFilterConfiguration
    where TFilter : class
    where TEntity : class
{
    private readonly FilterBuilder _builder;
    private readonly IGlobalExcludedPropertyManager? _globalExcluder;
    private readonly Dictionary<Type, MethodMetaData> _propertyEqualityCache;
    private readonly IEqualityMethodBuilder _equalityMethodBuilder;

    public FilterConfiguration(FilterBuilder builder, IEqualityMethodBuilder equalityMethodBuilder, IGlobalExcludedPropertyManager? globalExcluder, Dictionary<Type, MethodMetaData> propertyEqualityCache)
    {
        _builder = builder;
        _globalExcluder = globalExcluder;
        _propertyEqualityCache = propertyEqualityCache;
        _equalityMethodBuilder = equalityMethodBuilder;
    }

    public Dictionary<string, (string, FilteringType)> FilterDictionary { get; } = new ();

    public HashSet<string> ExcludedEntityProperties { get; } = new ();

    public Dictionary<string, Delegate> ExcludedFilterPropertiesByCondition { get; } = new ();

    public Dictionary<string, ExcludingOption> ExcludedFilterPropertiesByOption { get; } = new ();

    public bool IsEntityPropertyExcluded(string propertyName) =>
        (_globalExcluder != null && _globalExcluder.IsEntityPropertyExcluded(typeof(TEntity), propertyName)) || ExcludedEntityProperties.Contains(propertyName);

    public (string, FilteringType) GetFilteringType(string propertyName)
    {
        return FilterDictionary.TryGetValue(propertyName, out var tp) ? tp : (propertyName, FilteringType.Default);
    }

    public bool IsFilterPropertyExcluded(string propertyName) =>
        (_globalExcluder != null && _globalExcluder.IsFilterPropertyExcluded(typeof(TFilter), propertyName)) ||
        (ExcludedFilterPropertiesByOption.TryGetValue(propertyName, out var option) && option == ExcludingOption.Always);

    public IFilterPropertyExcludeByValueManager? GetFilterPropertyExcludeByValueManager()
    {
        var globalFilterPropertyExcludeByValueManager = _globalExcluder?.FilterPropertyExcludeByValueManager;
        return globalFilterPropertyExcludeByValueManager != null || ExcludedFilterPropertiesByCondition != null || ExcludedFilterPropertiesByOption != null
            ? new FilterPropertyExcludeByValueManager<TFilter>(_builder.EqualityManager, globalFilterPropertyExcludeByValueManager, ExcludedFilterPropertiesByCondition, ExcludedFilterPropertiesByOption)
            : null;
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> Configure<TFilterProperty, TEntityProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilteringType filteringType = FilteringType.Default)
    {
        var filterProperty = Utilities.GetProperty(filterPropertyExpression);
        var entityProperty = Utilities.GetProperty(entityPropertyExpression);
        if (!filterProperty.PropertyType.Matches(entityProperty.PropertyType))
        {
            throw new PropertyMatchingException(filterProperty.PropertyType, filterProperty.Name, entityProperty.PropertyType, entityProperty.Name);
        }

        if (!FilterDictionary.ContainsKey(filterProperty.Name))
        {
            throw new RedundantMatchingException(filterProperty.PropertyType, filterProperty.Name, entityProperty.PropertyType, entityProperty.Name);
        }

        FilterDictionary.Add(filterProperty.Name, (entityProperty.Name, filteringType));
        return this;
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> ExcludeEntityProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TEntity);
        if (_globalExcluder != null && _globalExcluder.IsEntityPropertyExcluded(type, propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        if (!ExcludedEntityProperties.Add(propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        return this;
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> ExcludeFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, Func<TProperty, bool> condition)
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TFilter);
        if (ExcludedFilterPropertiesByOption.ContainsKey(propertyName) || ExcludedFilterPropertiesByCondition.ContainsKey(propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        ExcludedFilterPropertiesByCondition.Add(propertyName, condition);
        return this;
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> ExcludeFilterProperty<TProperty>(Expression<Func<TFilter, TProperty>> propertyExpression, ExcludingOption option)
    {
        var property = Utilities.GetProperty(propertyExpression);
        var propertyName = property.Name;
        var type = typeof(TFilter);
        if (ExcludedFilterPropertiesByCondition.ContainsKey(propertyName) || ExcludedFilterPropertiesByOption.ContainsKey(propertyName))
        {
            throw new RedundantExcludingException(type, propertyName);
        }

        var properType = typeof(TProperty);
        if (option == ExcludingOption.DefaultValue && !_propertyEqualityCache.ContainsKey(properType))
        {
            _propertyEqualityCache.Add(properType, _equalityMethodBuilder.BuildEqualityMethod(properType));
        }

        return this;
    }

    public IFilterBuilder Finish()
    {
        foreach (var kvp in FilterDictionary)
        {
            if (IsFilterPropertyExcluded(kvp.Key))
            {
                throw new FilteringPropertyExcludedException(typeof(TFilter), kvp.Key);
            }

            if (IsEntityPropertyExcluded(kvp.Value.Item1))
            {
                throw new FilteringPropertyExcludedException(typeof(TEntity), kvp.Value.Item1);
            }
        }

        _builder.Register<TFilter, TEntity>(this);
        return _builder;
    }
}

internal sealed class FilterBuilder : IFilterBuilder
{
    private readonly IFilterBuilderConfiguration? _filterGlobalConfiguration;
    private readonly Dictionary<Type, Dictionary<Type, (MethodMetaData, IFilterPropertyExcludeByValueManager?)>> _expressionBuilderCache = new ();
    private readonly Dictionary<Type, MethodMetaData> _propertyEqualityCache;
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;

    public FilterBuilder(DynamicMethodBuilder dynamicMethodBuilder, FilterBuilderConfiguration? filterGlobalConfiguration, Dictionary<Type, Delegate> equalityManager)
    {
        EqualityManager = equalityManager;
        _dynamicMethodBuilder = dynamicMethodBuilder;
        _filterGlobalConfiguration = filterGlobalConfiguration;
        _propertyEqualityCache = filterGlobalConfiguration?.PropertyTypeEqualityDict ?? new ();
    }

    internal Dictionary<Type, Delegate> EqualityManager { get; }

    public IFilter Build()
    {
        var type = _dynamicMethodBuilder.Build();
        foreach (var kvp in _propertyEqualityCache)
        {
            EqualityManager.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name, BindingFlags.Public | BindingFlags.Static)));
        }

        return new Filter(_expressionBuilderCache, type);
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        if (_expressionBuilderCache.Contains(typeof(TFilter), typeof(TEntity)))
        {
            throw new RedundantRegisterException(typeof(TFilter), typeof(TEntity));
        }

        return new FilterConfiguration<TFilter, TEntity>(this, _dynamicMethodBuilder, _filterGlobalConfiguration, _propertyEqualityCache);
    }

    public void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        var filterType = typeof(TFilter);
        var entityType = typeof(TEntity);
        var manager = _filterGlobalConfiguration?.FilterPropertyExcludeByValueManager;
        if (!_expressionBuilderCache.AddIfNotExists(
            filterType,
            entityType,
            () => (
                _dynamicMethodBuilder.BuildUpFilterMethod(filterType, entityType, _filterGlobalConfiguration),
                manager == null ? null : new FilterPropertyExcludeByValueManager<TFilter>(EqualityManager, manager))))
        {
            throw new RedundantRegisterException(filterType, entityType);
        }
    }

    internal void Register<TFilter, TEntity>(IFilterConfiguration configuration)
        where TFilter : class
        where TEntity : class
    {
        var filterType = typeof(TFilter);
        var entityType = typeof(TEntity);
        if (!_expressionBuilderCache.AddIfNotExists(
            filterType,
            entityType,
            () => (
                _dynamicMethodBuilder.BuildUpFilterMethod(filterType, entityType, configuration),
                configuration.GetFilterPropertyExcludeByValueManager())))
        {
            throw new RedundantRegisterException(filterType, entityType);
        }
    }
}
