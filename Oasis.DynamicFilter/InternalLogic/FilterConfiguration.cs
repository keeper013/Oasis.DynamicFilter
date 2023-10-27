namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;

internal sealed class FilterConfiguration<TEntity, TFilter> : IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private readonly FilterBuilder _builder;
    private readonly FilterTypeBuilder _filterTypeBuilder;
    private readonly StringOperator? _defaultStringOperator;
    private readonly HashSet<string> _configuredEntityProperties = new ();
    private readonly List<CompareData<TFilter>> _compareList = new ();
    private readonly List<ContainData<TFilter>> _containList = new ();
    private readonly List<InData<TFilter>> _inList = new ();
    private readonly List<CompareStringData<TFilter>> _compareStringList = new ();
    private readonly List<FilterRangeData<TFilter>> _filterRangeList = new ();
    private readonly List<EntityRangeData<TFilter>> _entityRangeList = new ();
    private uint _fieldIndex = 0;

    public FilterConfiguration(FilterBuilder builder, StringOperator? defaultStringOperator, FilterTypeBuilder filterTypeBuilder)
    {
        _builder = builder;
        _defaultStringOperator = defaultStringOperator;
        _filterTypeBuilder = filterTypeBuilder;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        Operator type,
        Func<TFilter, TFilterProperty> filterPropertyFunc,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        var entityPropertyType = typeof(TEntityProperty);
        var filterPropertyType = typeof(TFilterProperty);

        if (!(entityPropertyType.IsInterface || entityPropertyType.IsClass || entityPropertyType.IsNullable(out _)) && includeNull != null)
        {
            throw new UnnecessaryIncludeNullException(entityPropertyType);
        }

        switch (type)
        {
            case Operator.In:
            case Operator.NotIn:
                var inData = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType) ?? throw new InvalidContainException(filterPropertyType, entityPropertyType);
                _inList.Add(new InData<TFilter>(_fieldIndex++, entityPropertyExpression, entityPropertyType, inData.itemConvertTo, type, filterPropertyFunc, filterPropertyType, inData.containerItemType, inData.isCollection, inData.nullValueNotCovered, includeNull, reverseIf, ignoreIf));
                break;
            case Operator.Contains:
            case Operator.NotContains:
                var containData = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType) ?? throw new InvalidContainException(filterPropertyType, entityPropertyType);
                if (ignoreIf is null && (filterPropertyType.IsInterface || filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
                {
                    ignoreIf = (TFilter filter) => filterPropertyFunc(filter) == null;
                }

                _containList.Add(new ContainData<TFilter>(_fieldIndex++, entityPropertyExpression, entityPropertyType, containData.containerItemType, type, filterPropertyFunc, filterPropertyType, containData.itemConvertTo, containData.isCollection, containData.nullValueNotCovered, includeNull, reverseIf, ignoreIf));
                break;
            default:
                var conversion = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, type) ?? throw new InvalidComparisonException(entityPropertyType, type, filterPropertyType);
                if (ignoreIf is null && (filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
                {
                    ignoreIf = (TFilter filter) => filterPropertyFunc(filter) == null;
                }

                _compareList.Add(new CompareData<TFilter>(_fieldIndex++, entityPropertyExpression, entityPropertyType, conversion.leftConvertTo, type, filterPropertyFunc, filterPropertyType, conversion.rightConvertTo, includeNull, reverseIf, ignoreIf));
                break;
        }

        var entityPropertyName = GetPropertyName(entityPropertyExpression);
        if (entityPropertyName != null)
        {
            _configuredEntityProperties.Add(entityPropertyName);
        }

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByStringProperty(
        Expression<Func<TEntity, string?>> entityPropertyExpression,
        StringOperator type,
        Func<TFilter, string?> filterPropertyFunc,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        if (ignoreIf is null)
        {
            ignoreIf = (TFilter filter) => filterPropertyFunc(filter) == null;
        }

        _compareStringList.Add(new CompareStringData<TFilter>(_fieldIndex++, entityPropertyExpression, type, filterPropertyFunc, includeNull, reverseIf, ignoreIf));

        var entityPropertyName = GetPropertyName(entityPropertyExpression);
        if (entityPropertyName != null)
        {
            _configuredEntityProperties.Add(entityPropertyName);
        }

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRangedFilter<TEntityProperty, TMinFilterProperty, TMaxFilterProperty>(
        Func<TFilter, TMinFilterProperty> filterMinPropertyFunc,
        RangeOperator minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        RangeOperator maxFilteringType,
        Func<TFilter, TMaxFilterProperty> filterMaxPropertyFunc,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null)
    {
        var entityPropertyType = typeof(TEntityProperty);
        var minFilterPropertyType = typeof(TMinFilterProperty);
        var maxFilterPropertyType = typeof(TMaxFilterProperty);
        var minConversion = TypeUtilities.GetComparisonConversion(minFilterPropertyType, entityPropertyType, ToOperator(minFilteringType)) ?? throw new InvalidComparisonException(minFilterPropertyType, ToOperator(minFilteringType), entityPropertyType);
        var maxConversion = TypeUtilities.GetComparisonConversion(entityPropertyType, maxFilterPropertyType, ToOperator(maxFilteringType)) ?? throw new InvalidComparisonException(entityPropertyType, ToOperator(maxFilteringType), maxFilterPropertyType);
        if (ignoreMinIf is null && (minFilterPropertyType.IsClass || minFilterPropertyType.IsNullable(out _)))
        {
            ignoreMinIf = (TFilter filter) => filterMinPropertyFunc(filter) == null;
        }

        if (ignoreMaxIf is null && (maxFilterPropertyType.IsClass || maxFilterPropertyType.IsNullable(out _)))
        {
            ignoreMaxIf = (TFilter filter) => filterMaxPropertyFunc(filter) == null;
        }

        if (!(entityPropertyType.IsClass || entityPropertyType.IsNullable(out _)) && includeNull != null)
        {
            throw new UnnecessaryIncludeNullException(entityPropertyType);
        }

        _filterRangeList.Add(new FilterRangeData<TFilter>(_fieldIndex++, filterMinPropertyFunc, minFilterPropertyType, minConversion.leftConvertTo, minFilteringType, minConversion.rightConvertTo, entityPropertyExpression, entityPropertyType, maxConversion.leftConvertTo, maxFilteringType, maxConversion.rightConvertTo, filterMaxPropertyFunc, maxFilterPropertyType, includeNull, reverseIf, ignoreMinIf, ignoreMaxIf));
        var entityPropertyName = GetPropertyName(entityPropertyExpression);
        if (entityPropertyName != null)
        {
            _configuredEntityProperties.Add(entityPropertyName);
        }

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRangedEntity<TMinEntityProperty, TFilterProperty, TMaxEntityProperty>(
        Expression<Func<TEntity, TMinEntityProperty>> entityPropertyMinExpression,
        RangeOperator minFilteringType,
        Func<TFilter, TFilterProperty> filterPropertyFunc,
        RangeOperator maxFilteringType,
        Expression<Func<TEntity, TMaxEntityProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? includeNullMin = null,
        Func<TFilter, bool>? includeNullMax = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        var minEntityPropertyType = typeof(TMinEntityProperty);
        var filterPropertyType = typeof(TFilterProperty);
        var maxEntityPropertyType = typeof(TMaxEntityProperty);
        var minConversion = TypeUtilities.GetComparisonConversion(minEntityPropertyType, filterPropertyType, ToOperator(minFilteringType)) ?? throw new InvalidComparisonException(minEntityPropertyType, ToOperator(minFilteringType), filterPropertyType);
        var maxConversion = TypeUtilities.GetComparisonConversion(filterPropertyType, maxEntityPropertyType, ToOperator(maxFilteringType)) ?? throw new InvalidComparisonException(filterPropertyType, ToOperator(maxFilteringType), maxEntityPropertyType);
        if (ignoreIf is null && (filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
        {
            ignoreIf = (TFilter filter) => filterPropertyFunc(filter) == null;
        }

        if (!(minEntityPropertyType.IsClass || minEntityPropertyType.IsNullable(out _)) && includeNullMin != null)
        {
            throw new UnnecessaryIncludeNullException(minEntityPropertyType);
        }

        if (!(maxEntityPropertyType.IsClass || maxEntityPropertyType.IsNullable(out _)) && includeNullMax != null)
        {
            throw new UnnecessaryIncludeNullException(maxEntityPropertyType);
        }

        _entityRangeList.Add(new EntityRangeData<TFilter>(_fieldIndex++, entityPropertyMinExpression, minEntityPropertyType, minConversion.leftConvertTo, minFilteringType, minConversion.rightConvertTo, filterPropertyFunc, filterPropertyType, maxConversion.leftConvertTo, maxFilteringType, maxConversion.rightConvertTo, entityPropertyMaxExpression, maxEntityPropertyType, includeNullMin, includeNullMax, reverseIf, ignoreIf));

        var minEntityPropertyName = GetPropertyName(entityPropertyMinExpression);
        if (minEntityPropertyName != null)
        {
            _configuredEntityProperties.Add(minEntityPropertyName);
        }

        var maxEntityPropertyName = GetPropertyName(entityPropertyMaxExpression);
        if (maxEntityPropertyName != null)
        {
            _configuredEntityProperties.Add(maxEntityPropertyName);
        }

        return this;
    }

    public IFilterBuilder Finish()
    {
        _builder.Add(typeof(TEntity), typeof(TFilter), BuildTypeAndFunction());
        return _builder;
    }

    private static string? GetPropertyName<TClass, TProperty>(Expression<Func<TClass, TProperty>> expression)
        where TClass : class
    {
        MemberExpression? memberExpression = null;
        if (expression.Body.NodeType == ExpressionType.Convert)
        {
            var body = (UnaryExpression)expression.Body;
            memberExpression = body.Operand as MemberExpression;
        }
        else if (expression.Body.NodeType == ExpressionType.MemberAccess)
        {
            memberExpression = expression.Body as MemberExpression;
        }

        if (memberExpression == null)
        {
            return null;
        }

        if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
        {
            return null;
        }

        return (memberExpression.Member as PropertyInfo)?.Name;
    }

    private static Operator ToOperator(RangeOperator type) => type == RangeOperator.LessThan ? Operator.LessThan : Operator.LessThanOrEqual;

    private Delegate BuildTypeAndFunction()
    {
        var type = _filterTypeBuilder.BuildFilterMethodBuilder<TEntity, TFilter>(_defaultStringOperator).Build(
            _fieldIndex,
            _configuredEntityProperties,
            _compareList,
            _containList,
            _inList,
            _compareStringList,
            _filterRangeList,
            _entityRangeList);
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TEntity), typeof(bool))));
        return Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder.FilterMethodName, Utilities.PublicStatic));
    }
}