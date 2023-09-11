namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

internal interface IFilterConfiguration
{
    HashSet<string> ExcludedEntityProperties { get; }

    Dictionary<string, Delegate> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<string, ExcludingOption> ExcludedFilterPropertiesByOption { get; }

    public Dictionary<string, (string, FilteringType)> FilterDictionary { get; }

    bool IsEntityPropertyExcluded(string propertyName);

    bool IsFilterPropertyAlwaysExcluded(string propertyName);
}

internal sealed class FilterConfiguration<TFilter, TEntity> : IFilterConfigurationBuilder<TFilter, TEntity>, IFilterConfiguration
    where TFilter : class
    where TEntity : class
{
    private readonly FilterBuilder _builder;
    private readonly IGlobalPropertyExcluder? _globalExcluder;

    public FilterConfiguration(FilterBuilder builder, IGlobalPropertyExcluder? globalExcluder)
    {
        _builder = builder;
        _globalExcluder = globalExcluder;
    }

    public Dictionary<string, (string, FilteringType)> FilterDictionary { get; } = new ();

    public HashSet<string> ExcludedEntityProperties { get; } = new ();

    public Dictionary<string, Delegate> ExcludedFilterPropertiesByCondition { get; } = new ();

    public Dictionary<string, ExcludingOption> ExcludedFilterPropertiesByOption { get; } = new ();

    public bool IsEntityPropertyExcluded(string propertyName) =>
        (_globalExcluder != null && _globalExcluder.IsEntityPropertyExcluded(typeof(TEntity), propertyName)) || ExcludedEntityProperties.Contains(propertyName);

    public bool IsFilterPropertyAlwaysExcluded(string propertyName) =>
        (_globalExcluder != null && _globalExcluder.IsFilterPropertyAlwaysExcluded(typeof(TFilter), propertyName)) ||
        (ExcludedFilterPropertiesByOption.TryGetValue(propertyName, out var option) && option == ExcludingOption.Always);

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

        return this;
    }

    public IFilterBuilder Finish()
    {
        foreach (var kvp in FilterDictionary)
        {
            if (IsFilterPropertyAlwaysExcluded(kvp.Key))
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
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly IFilterBuilderConfiguration? _filterGlobalConfiguration;
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _expressionBuilderCache = new ();

    public FilterBuilder(IFilterBuilderConfiguration? filterGlobalConfiguration)
    {
        var name = new AssemblyName($"{GenerateRandomTypeName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new (module.DefineType("Mapper", TypeAttributes.Public));
        _filterGlobalConfiguration = filterGlobalConfiguration;
    }

    public IFilter Build()
    {
        var type = _dynamicMethodBuilder.Build();
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

        return new FilterConfiguration<TFilter, TEntity>(this, _filterGlobalConfiguration);
    }

    public void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        var filterType = typeof(TFilter);
        var entityType = typeof(TEntity);
        if (!_expressionBuilderCache.AddIfNotExists(
            filterType,
            entityType,
            () => _dynamicMethodBuilder.BuildUpFilterMethod(filterType, entityType, _filterGlobalConfiguration)))
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
            () => _dynamicMethodBuilder.BuildUpFilterMethod(filterType, entityType, _filterGlobalConfiguration, configuration)))
        {
            throw new RedundantRegisterException(filterType, entityType);
        }
    }

    private static string GenerateRandomTypeName(int length)
    {
        const string AvailableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int AvailableCharsCount = 52;
        var bytes = new byte[length];
        RandomNumberGenerator.Create().GetBytes(bytes);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }
}
