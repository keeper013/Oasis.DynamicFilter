namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;
using System.Linq;

internal record struct FilterRangeData<TFilter>(
    PropertyInfo filterMinProperty,
    Type? filterMinPropertyConvertTo,
    RangeOperator filterMinType,
    Type? entityMinPropertyConvertTo,
    PropertyInfo entityProperty,
    Type? entityMaxPropertyConvertTo,
    RangeOperator filterMaxType,
    Type? filterMaxPropertyConvertTo,
    PropertyInfo filterMaxProperty,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreMinIf,
    Func<TFilter, bool>? ignoreMaxIf)
    where TFilter : class;

internal record struct EntityRangeData<TFilter>(
    PropertyInfo entityMinProperty,
    Type? entityMinPropertyConvertTo,
    RangeOperator entityMinType,
    Type? filterMinPropertyConvertTo,
    PropertyInfo filterProperty,
    Type? filterMaxPropertyConvertTo,
    RangeOperator entityMaxType,
    Type? entityMaxPropertyConvertTo,
    PropertyInfo entityMaxProperty,
    Func<TFilter, bool>? includeNullMin,
    Func<TFilter, bool>? includeNullMax,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal sealed class FilterConfiguration<TEntity, TFilter> : IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private readonly FilterBuilder _builder;
    private readonly FilterTypeBuilder _filterTypeBuilder;
    private readonly HashSet<string> _configuredEntityProperties = new ();
    private readonly HashSet<string> _configuredFilterProperties = new ();
    private readonly Dictionary<string, Dictionary<string, CompareData<TFilter>>> _compareDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, ContainData<TFilter>>> _containDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, InData<TFilter>>> _inDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, CompareStringData<TFilter>>> _compareStringDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, FilterRangeData<TFilter>>>> _filterRangeDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, EntityRangeData<TFilter>>>> _entityRangeDictionary = new ();

    public FilterConfiguration(FilterBuilder builder, FilterTypeBuilder filterTypeBuilder)
    {
        _builder = builder;
        _filterTypeBuilder = filterTypeBuilder;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        Operator type,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        var entityProperty = GetProperty(entityPropertyExpression);
        var filterProperty = GetProperty(filterPropertyExpression);
        var entityPropertyName = entityProperty.Name;
        var filterPropertyName = filterProperty.Name;

        if (_compareDictionary.Contains(entityPropertyName, filterPropertyName) || _containDictionary.Contains(entityPropertyName, filterPropertyName) || _inDictionary.Contains(entityPropertyName, filterPropertyName))
        {
            throw new RedundantMatchingException(typeof(TEntity), entityProperty.Name, typeof(TFilter), filterProperty.Name);
        }

        var entityPropertyType = entityProperty.PropertyType;
        var filterPropertyType = filterProperty.PropertyType;

        if (!(entityPropertyType.IsInterface || entityPropertyType.IsClass || entityPropertyType.IsNullable(out _)) && includeNull != null)
        {
            throw new UnnecessaryIncludeNullException(entityPropertyType);
        }

        switch (type)
        {
            case Operator.In:
            case Operator.NotIn:
                var inData = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType) ?? throw new InvalidContainException(filterPropertyType, entityPropertyType);
                _inDictionary.Add(
                    entityPropertyName,
                    filterPropertyName,
                    new InData<TFilter>(entityProperty, inData.itemConvertTo, type, filterProperty, inData.containerItemType, inData.isCollection, inData.nullValueNotCovered, includeNull, reverseIf, ignoreIf));
                break;
            case Operator.Contains:
            case Operator.NotContains:
                var containData = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType) ?? throw new InvalidContainException(filterPropertyType, entityPropertyType);
                if (ignoreIf is null && (filterPropertyType.IsInterface || filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
                {
                    ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
                }

                _containDictionary.Add(
                    entityPropertyName,
                    filterPropertyName,
                    new ContainData<TFilter>(entityProperty, containData.containerItemType, type, filterProperty, containData.itemConvertTo, containData.isCollection, containData.nullValueNotCovered, includeNull, reverseIf, ignoreIf));
                break;
            default:
                var conversion = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, type) ?? throw new InvalidComparisonException(typeof(TEntity), entityPropertyName, type, typeof(TFilter), filterPropertyName);
                if (ignoreIf is null && (filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
                {
                    ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
                }

                _compareDictionary.Add(entityPropertyName, filterPropertyName, new CompareData<TFilter>(entityProperty, conversion.leftConvertTo, type, filterProperty, conversion.rightConvertTo, includeNull, reverseIf, ignoreIf));
                break;
        }

        _configuredEntityProperties.Add(entityPropertyName);
        _configuredFilterProperties.Add(filterPropertyName);

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByStringProperty(
        Expression<Func<TEntity, string?>> entityPropertyExpression,
        StringOperator type,
        Expression<Func<TFilter, string?>> filterPropertyExpression,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        var entityProperty = GetProperty(entityPropertyExpression);
        var filterProperty = GetProperty(filterPropertyExpression);
        var entityPropertyName = entityProperty.Name;
        var filterPropertyName = filterProperty.Name;

        if (_compareDictionary.Contains(entityPropertyName, filterPropertyName) || _containDictionary.Contains(entityPropertyName, filterPropertyName)
            || _inDictionary.Contains(entityPropertyName, filterPropertyName) || _compareStringDictionary.Contains(entityPropertyName, filterPropertyName))
        {
            throw new RedundantMatchingException(typeof(TEntity), entityPropertyName, typeof(TFilter), filterPropertyName);
        }

        if (entityProperty.PropertyType != typeof(string) || filterProperty.PropertyType != typeof(string))
        {
            throw new InvalidStringTypeException(typeof(TEntity), entityPropertyName, typeof(TFilter), filterPropertyName);
        }

        if (ignoreIf is null)
        {
            ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
        }

        _compareStringDictionary.Add(entityPropertyName, filterPropertyName, new CompareStringData<TFilter>(entityProperty, type, filterProperty, includeNull, reverseIf, ignoreIf));
        _configuredEntityProperties.Add(entityPropertyName);
        _configuredFilterProperties.Add(filterPropertyName);

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRangedFilter<TEntityProperty, TMinFilterProperty, TMaxFilterProperty>(
        Expression<Func<TFilter, TMinFilterProperty>> filterPropertyMinExpression,
        RangeOperator minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        RangeOperator maxFilteringType,
        Expression<Func<TFilter, TMaxFilterProperty>> filterPropertyMaxExpression,
        Func<TFilter, bool>? includeNull = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null)
    {
        var minFilterProperty = GetProperty(filterPropertyMinExpression);
        var entityProperty = GetProperty(entityPropertyExpression);
        var maxFilterProperty = GetProperty(filterPropertyMaxExpression);
        if (_filterRangeDictionary.Contains(minFilterProperty.Name, entityProperty.Name, maxFilterProperty.Name))
        {
            throw new RedundantMatchingException(typeof(TEntity), entityProperty.Name, typeof(TMaxFilterProperty), minFilterProperty.Name, maxFilterProperty.Name);
        }

        var entityPropertyType = typeof(TEntityProperty);
        var minFilterPropertyType = typeof(TMinFilterProperty);
        var maxFilterPropertyType = typeof(TMaxFilterProperty);
        var minConversion = TypeUtilities.GetComparisonConversion(minFilterPropertyType, entityPropertyType, ToOperator(minFilteringType)) ?? throw new InvalidComparisonException(typeof(TFilter), minFilterProperty.Name, ToOperator(minFilteringType), typeof(TEntity), entityProperty.Name);
        var maxConversion = TypeUtilities.GetComparisonConversion(entityPropertyType, maxFilterPropertyType, ToOperator(maxFilteringType)) ?? throw new InvalidComparisonException(typeof(TEntity), entityProperty.Name, ToOperator(maxFilteringType), typeof(TFilter), maxFilterProperty.Name);
        if (ignoreMinIf is null && (minFilterPropertyType.IsClass || minFilterPropertyType.IsNullable(out _)))
        {
            ignoreMinIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(minFilterProperty);
        }

        if (ignoreMaxIf is null && (maxFilterPropertyType.IsClass || maxFilterPropertyType.IsNullable(out _)))
        {
            ignoreMaxIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(maxFilterProperty);
        }

        if (!(entityPropertyType.IsClass || entityPropertyType.IsNullable(out _)) && includeNull != null)
        {
            throw new UnnecessaryIncludeNullException(entityPropertyType);
        }

        _filterRangeDictionary.Add(
            minFilterProperty.Name,
            entityProperty.Name,
            maxFilterProperty.Name,
            new FilterRangeData<TFilter>(minFilterProperty, minConversion.leftConvertTo, minFilteringType, minConversion.rightConvertTo, entityProperty, maxConversion.leftConvertTo, maxFilteringType, maxConversion.rightConvertTo, maxFilterProperty, includeNull, reverseIf, ignoreMinIf, ignoreMaxIf));
        _configuredEntityProperties.Add(entityProperty.Name);
        _configuredFilterProperties.Add(minFilterProperty.Name);
        _configuredFilterProperties.Add(maxFilterProperty.Name);

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRangedEntity<TMinEntityProperty, TFilterProperty, TMaxEntityProperty>(
        Expression<Func<TEntity, TMinEntityProperty>> entityPropertyMinExpression,
        RangeOperator minFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        RangeOperator maxFilteringType,
        Expression<Func<TEntity, TMaxEntityProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? includeNullMin = null,
        Func<TFilter, bool>? includeNullMax = null,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        var minEntityProperty = GetProperty(entityPropertyMinExpression);
        var filterProperty = GetProperty(filterPropertyExpression);
        var maxEntityProperty = GetProperty(entityPropertyMaxExpression);

        if (_entityRangeDictionary.Contains(minEntityProperty.Name, filterProperty.Name, maxEntityProperty.Name))
        {
            throw new RedundantMatchingException(typeof(TEntity), minEntityProperty.Name, maxEntityProperty.Name, typeof(TFilter), filterProperty.Name);
        }

        var minEntityPropertyType = typeof(TMinEntityProperty);
        var filterPropertyType = typeof(TFilterProperty);
        var maxEntityPropertyType = typeof(TMaxEntityProperty);
        var minConversion = TypeUtilities.GetComparisonConversion(minEntityPropertyType, filterPropertyType, ToOperator(minFilteringType)) ?? throw new InvalidComparisonException(typeof(TEntity), minEntityProperty.Name, ToOperator(minFilteringType), typeof(TFilter), filterProperty.Name);
        var maxConversion = TypeUtilities.GetComparisonConversion(filterPropertyType, maxEntityPropertyType, ToOperator(maxFilteringType)) ?? throw new InvalidComparisonException(typeof(TFilter), filterProperty.Name, ToOperator(maxFilteringType), typeof(TEntity), maxEntityProperty.Name);
        if (ignoreIf is null && (filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)))
        {
            ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
        }

        if (!(minEntityPropertyType.IsClass || minEntityPropertyType.IsNullable(out _)) && includeNullMin != null)
        {
            throw new UnnecessaryIncludeNullException(minEntityPropertyType);
        }

        if (!(maxEntityPropertyType.IsClass || maxEntityPropertyType.IsNullable(out _)) && includeNullMax != null)
        {
            throw new UnnecessaryIncludeNullException(maxEntityPropertyType);
        }

        _entityRangeDictionary.Add(
            minEntityProperty.Name,
            filterProperty.Name,
            maxEntityProperty.Name,
            new EntityRangeData<TFilter>(minEntityProperty, minConversion.leftConvertTo, minFilteringType, minConversion.rightConvertTo, filterProperty, maxConversion.leftConvertTo, maxFilteringType, maxConversion.rightConvertTo, maxEntityProperty, includeNullMin, includeNullMax, reverseIf, ignoreIf));
        _configuredEntityProperties.Add(minEntityProperty.Name);
        _configuredEntityProperties.Add(maxEntityProperty.Name);
        _configuredFilterProperties.Add(filterProperty.Name);

        return this;
    }

    public IFilterBuilder Finish()
    {
        _builder.Add(typeof(TEntity), typeof(TFilter), BuildTypeAndFunction());
        return _builder;
    }

    private static PropertyInfo GetProperty<TClass, TProperty>(Expression<Func<TClass, TProperty>> expression)
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
            throw new ArgumentException("Not a member access", nameof(expression));
        }

        var member = memberExpression.Member;
        var property = member as PropertyInfo;
        return property ?? throw new InvalidOperationException(string.Format("Member with Name '{0}' is not a property.", member.Name));
    }

    private static Operator ToOperator(RangeOperator type) => type == RangeOperator.LessThan ? Operator.LessThan : Operator.LessThanOrEqual;

    private Delegate BuildTypeAndFunction()
    {
        var type = _filterTypeBuilder.BuildFilterMethodBuilder<TEntity, TFilter>().Build(
            _configuredEntityProperties,
            _configuredFilterProperties,
            _compareDictionary.Values.SelectMany(c => c.Values).ToList(),
            _containDictionary.Values.SelectMany(c => c.Values).ToList(),
            _inDictionary.Values.SelectMany(i => i.Values).ToList(),
            _compareStringDictionary.Values.SelectMany(c => c.Values).ToList(),
            _filterRangeDictionary.Values.SelectMany(f => f.Values.SelectMany(i => i.Values)).ToList(),
            _entityRangeDictionary.Values.SelectMany(e => e.Values.SelectMany(i => i.Values)).ToList());
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TEntity), typeof(bool))));
        return Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder.FilterMethodName, Utilities.PublicStatic));
    }
}