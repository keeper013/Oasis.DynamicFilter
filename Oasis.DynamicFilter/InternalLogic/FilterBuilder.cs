﻿namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

internal interface IFilterConfiguration : IExcludedProperties
{
    public Dictionary<string, Dictionary<string, FilteringType>> FilterDictionary { get; }
}

internal sealed class FilterConfiguration<TFilter, TEntity> : PropertyExcluder<IFilterConfiguration<TFilter, TEntity>>, IFilterConfiguration<TFilter, TEntity>, IFilterConfiguration
    where TFilter : class
    where TEntity : class
{
    private readonly FilterBuilder _builder;

    public FilterConfiguration(FilterBuilder builder)
    {
        _builder = builder;
    }

    public Dictionary<string, Dictionary<string, FilteringType>> FilterDictionary { get; } = new ();

    public IFilterConfiguration<TFilter, TEntity> Configure<TFilterProperty, TEntityProperty>(
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

        if (!FilterDictionary.AddIfNotExists(filterProperty.Name, entityProperty.Name, filteringType))
        {
            throw new RedundantMatchingException(filterProperty.PropertyType, filterProperty.Name, entityProperty.PropertyType, entityProperty.Name);
        }

        return this;
    }

    public IFilterBuilder Finish()
    {
        _builder.Register<TFilter, TEntity>(this);
        return _builder;
    }
}

internal sealed class FilterBuilder : IFilterBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly IFilterGlobalConfiguration? _filterGlobalConfiguration;
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _expressionBuilderCache = new ();

    public FilterBuilder(IFilterGlobalConfiguration? filterGlobalConfiguration)
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

    public IFilterConfiguration<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        if (_expressionBuilderCache.Contains(typeof(TFilter), typeof(TEntity)))
        {
            throw new RedundantRegisterException(typeof(TFilter), typeof(TEntity));
        }

        return new FilterConfiguration<TFilter, TEntity>(this);
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
