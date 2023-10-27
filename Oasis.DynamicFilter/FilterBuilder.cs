namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.Exceptions;
using Oasis.DynamicFilter.InternalLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

public sealed class FilterBuilder : IFilterBuilder
{
    private readonly FilterTypeBuilder _filterTypeBuilder;
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _filterBuilders = new ();

    public FilterBuilder(StringOperator defaultStringOperator = StringOperator.Equality)
    {
        var name = new AssemblyName($"{Utilities.GenerateRandomName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _filterTypeBuilder = new (module, defaultStringOperator);
    }

    public IFilter Build()
    {
        var dict = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var kvp1 in _filterBuilders)
        {
            dict.Add(kvp1.Key, kvp1.Value);
        }

        return new Filter(dict);
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> Configure<TEntity, TFilter>(StringOperator? defaultStringOperator = null)
        where TEntity : class
        where TFilter : class
    {
        if (_filterBuilders.Contains(typeof(TEntity), typeof(TFilter)))
        {
            throw new RedundantRegisterException(typeof(TEntity), typeof(TFilter));
        }

        return new FilterConfiguration<TEntity, TFilter>(this, defaultStringOperator, _filterTypeBuilder);
    }

    public IFilterBuilder Register<TEntity, TFilter>(StringOperator? defaultStringOperator = null)
        where TEntity : class
        where TFilter : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        if (_filterBuilders.Contains(entityType, typeof(TFilter)))
        {
            throw new RedundantRegisterException(entityType, filterType);
        }

        var type = _filterTypeBuilder.BuildFilterMethodBuilder<TEntity, TFilter>(defaultStringOperator).Build();
        var delegateType = typeof(Func<,>).MakeGenericType(filterType, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        _filterBuilders.Add(entityType, filterType, Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder.FilterMethodName, Utilities.PublicStatic)));

        return this;
    }

    internal void Add(Type entityType, Type filterType, Delegate filterBuilder) => _filterBuilders.Add(entityType, filterType, filterBuilder);
}
