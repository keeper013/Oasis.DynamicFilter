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
    private readonly ModuleBuilder _moduleBuilder;
    private readonly IDictionary<Type, IDictionary<Type, ICanConvertToDelegate>> _lazyFilterBuilders = new Dictionary<Type, IDictionary<Type, ICanConvertToDelegate>>();
    private readonly IDictionary<Type, IDictionary<Type, Delegate>> _filterBuilders = new Dictionary<Type, IDictionary<Type, Delegate>>();
    private readonly bool _defaultLazy;

    public FilterBuilder(bool defaultLazy = false)
    {
        var name = new AssemblyName($"{Utilities.GenerateRandomName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _defaultLazy = defaultLazy;
    }

    public IFilter Build(bool autoRegisterIfNot = false) => new Filter(_filterBuilders, _lazyFilterBuilders, autoRegisterIfNot ? _moduleBuilder : null);

    public IFilterConfigurationBuilder<TEntity, TFilter> Configure<TEntity, TFilter>(bool? isLazy = null)
        where TEntity : class
        where TFilter : class
    {
        if (_lazyFilterBuilders.Contains(typeof(TEntity), typeof(TFilter)))
        {
            throw new RedundantRegisterException(typeof(TEntity), typeof(TFilter));
        }

        return new FilterConfiguration<TEntity, TFilter>(this, new FilterTypeBuilder<TEntity, TFilter>(_moduleBuilder), isLazy ?? _defaultLazy);
    }

    public IFilterBuilder Register<TEntity, TFilter>()
        where TEntity : class
        where TFilter : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        if (_filterBuilders.Contains(entityType, filterType))
        {
            throw new RedundantRegisterException(entityType, filterType);
        }

        _filterBuilders.Add(entityType, filterType, BuildDelegate<TEntity, TFilter>(_moduleBuilder));
        return this;
    }

    internal static Delegate BuildDelegate<TEntity, TFilter>(ModuleBuilder moduleBuilder)
        where TEntity : class
        where TFilter : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);

        var type = new FilterTypeBuilder<TEntity, TFilter>(moduleBuilder).Build();
        if (type != null)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(filterType, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
            return Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder<TEntity, TFilter>.FilterMethodName, Utilities.PublicStatic));
        }
        else
        {
            throw new TrivialRegisterException(entityType, filterType);
        }
    }

    internal void Add(Type entityType, Type filterType, ICanConvertToDelegate filterConfiguration)
    {
        if (!filterConfiguration.IsLazy)
        {
            _filterBuilders.Add(entityType, filterType, filterConfiguration.ToDelegate());
        }
        else
        {
            _lazyFilterBuilders.Add(entityType, filterType, filterConfiguration);
        }
    }
}
