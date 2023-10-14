namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;
using System.Linq;

internal record struct CompareData<TFilter>(
    PropertyInfo entityProperty,
    Type? entityPropertyConvertTo,
    FilterByPropertyType type,
    PropertyInfo filterProperty,
    Type? filterPropertyConvertTo,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct ContainData<TFilter>(
    PropertyInfo entityProperty,
    Type entityPropertyItemType,
    FilterByPropertyType type,
    PropertyInfo filterProperty,
    Type? filterPropertyConvertTo,
    bool isCollection,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct InData<TFilter>(
    PropertyInfo entityProperty,
    Type? entityPropertyConvertTo,
    FilterByPropertyType type,
    PropertyInfo filterProperty,
    Type filterPropertyItemType,
    bool isCollection,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct FilterRangeData<TFilter>(
    PropertyInfo minFilterProperty,
    Type? minFilterPropertyConvertTo,
    FilterByRangeType minFilterType,
    Type? entityPropertyMinConvertTo,
    PropertyInfo entityProperty,
    Type? entityPropertyMaxConvertTo,
    FilterByRangeType maxFilterType,
    Type? maxFilterPropertyConvertTo,
    PropertyInfo maxFilterProperty,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreMinIf,
    Func<TFilter, bool>? ignoreMaxIf)
    where TFilter : class;

internal record struct EntityRangeData<TFilter>(
    PropertyInfo minEntityProperty,
    Type? minEntityPropertyConvertTo,
    FilterByRangeType minEntityType,
    Type? filterPropertyMinConvertTo,
    PropertyInfo filterProperty,
    Type? filterPropertyMaxConvertTo,
    FilterByRangeType maxEntityType,
    Type? maxEntityPropertyConvertTo,
    PropertyInfo maxEntityProperty,
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
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, FilterRangeData<TFilter>>>> _filterRangeDictionary = new ();
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, EntityRangeData<TFilter>>>> _entityRangeDictionary = new ();

    public FilterConfiguration(FilterBuilder builder, FilterTypeBuilder filterTypeBuilder)
    {
        _builder = builder;
        _filterTypeBuilder = filterTypeBuilder;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByPropertyType type,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
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
        if ((filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)) && ignoreIf is null)
        {
            ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
        }

        switch (type)
        {
            case FilterByPropertyType.In:
            case FilterByPropertyType.NotIn:
                var inData = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (inData == null)
                {
                    throw new InvalidContainException(filterPropertyType, entityPropertyType);
                }

                var inValue = inData.Value;
                _inDictionary.Add(entityPropertyName, filterPropertyName, new InData<TFilter>(entityProperty, inValue.Item2, type, filterProperty, inValue.Item1, inValue.Item3, reverseIf, ignoreIf));
                break;
            case FilterByPropertyType.Contains:
            case FilterByPropertyType.NotContains:
                var containData = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (containData == null)
                {
                    throw new InvalidContainException(filterPropertyType, entityPropertyType);
                }

                var containValue = containData.Value;
                _containDictionary.Add(entityPropertyName, filterPropertyName, new ContainData<TFilter>(entityProperty, containValue.Item1, type, filterProperty, containValue.Item2, containValue.Item3, reverseIf, ignoreIf));
                break;
            default:
                var conversion = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, type);
                if (conversion == null)
                {
                    throw new InvalidComparisonException(typeof(TEntity), entityPropertyName, type, typeof(TFilter), filterPropertyName);
                }

                var c = conversion.Value;

                _compareDictionary.Add(entityPropertyName, filterPropertyName, new CompareData<TFilter>(entityProperty, c.Item1, type, filterProperty, c.Item2, reverseIf, ignoreIf));
                break;
        }

        _configuredEntityProperties.Add(entityPropertyName);
        _configuredFilterProperties.Add(filterPropertyName);

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TEntityProperty, TMinFilterProperty, TMaxFilterProperty>(
        Expression<Func<TFilter, TMinFilterProperty>> filterPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TFilter, TMaxFilterProperty>> filterPropertyMaxExpression,
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
        var minConversion = TypeUtilities.GetComparisonConversion(minFilterPropertyType, entityPropertyType, ToFilterByPropertyType(minFilteringType));
        if (minConversion == null)
        {
            throw new InvalidComparisonException(typeof(TFilter), minFilterProperty.Name, ToFilterByPropertyType(minFilteringType), typeof(TEntity), entityProperty.Name);
        }

        var maxConversion = TypeUtilities.GetComparisonConversion(entityPropertyType, maxFilterPropertyType, ToFilterByPropertyType(maxFilteringType));
        if (maxConversion == null)
        {
            throw new InvalidComparisonException(typeof(TEntity), entityProperty.Name, ToFilterByPropertyType(maxFilteringType), typeof(TFilter), maxFilterProperty.Name);
        }

        if ((minFilterPropertyType.IsClass || minFilterPropertyType.IsNullable(out _)) && ignoreMinIf == null)
        {
            ignoreMinIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(minFilterProperty);
        }

        if ((maxFilterPropertyType.IsClass || maxFilterPropertyType.IsNullable(out _)) && ignoreMaxIf == null)
        {
            ignoreMaxIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(maxFilterProperty);
        }

        var min = minConversion.Value;
        var max = maxConversion.Value;
        _filterRangeDictionary.Add(
            minFilterProperty.Name,
            entityProperty.Name,
            maxFilterProperty.Name,
            new FilterRangeData<TFilter>(minFilterProperty, min.Item1, minFilteringType, min.Item2, entityProperty, max.Item2, maxFilteringType, max.Item2, maxFilterProperty, reverseIf, ignoreMinIf, ignoreMaxIf));
        _configuredEntityProperties.Add(entityProperty.Name);
        _configuredFilterProperties.Add(minFilterProperty.Name);
        _configuredFilterProperties.Add(maxFilterProperty.Name);

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TMinEntityProperty, TFilterProperty, TMaxEntityProperty>(
        Expression<Func<TEntity, TMinEntityProperty>> entityPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TEntity, TMaxEntityProperty>> entityPropertyMaxExpression,
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
        var minConversion = TypeUtilities.GetComparisonConversion(minEntityPropertyType, filterPropertyType, ToFilterByPropertyType(minFilteringType));
        if (minConversion == null)
        {
            throw new InvalidComparisonException(typeof(TEntity), minEntityProperty.Name, ToFilterByPropertyType(minFilteringType), typeof(TFilter), filterProperty.Name);
        }

        var maxConversion = TypeUtilities.GetComparisonConversion(filterPropertyType, maxEntityPropertyType, ToFilterByPropertyType(maxFilteringType));
        if (maxConversion == null)
        {
            throw new InvalidComparisonException(typeof(TFilter), filterProperty.Name, ToFilterByPropertyType(maxFilteringType), typeof(TEntity), maxEntityProperty.Name);
        }

        if ((filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)) && ignoreIf is null)
        {
            ignoreIf = TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty);
        }

        var min = minConversion.Value;
        var max = maxConversion.Value;
        _entityRangeDictionary.Add(
            minEntityProperty.Name,
            filterProperty.Name,
            maxEntityProperty.Name,
            new EntityRangeData<TFilter>(minEntityProperty, min.Item1, minFilteringType, min.Item2, filterProperty, max.Item2, maxFilteringType, max.Item2, maxEntityProperty, reverseIf, ignoreIf));
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
        return property == null
            ? throw new InvalidOperationException(string.Format("Member with Name '{0}' is not a property.", member.Name))
            : property;
    }

    private static FilterByPropertyType ToFilterByPropertyType(FilterByRangeType type) => type == FilterByRangeType.LessThan ? FilterByPropertyType.LessThan : FilterByPropertyType.LessThanOrEqual;

    private Delegate BuildTypeAndFunction()
    {
        var type = _filterTypeBuilder.BuildFilterMethodBuilder<TEntity, TFilter>().Build(
            _configuredEntityProperties,
            _configuredFilterProperties,
            _compareDictionary.Values.SelectMany(c => c.Values).ToList(),
            _containDictionary.Values.SelectMany(c => c.Values).ToList(),
            _inDictionary.Values.SelectMany(i => i.Values).ToList(),
            _filterRangeDictionary.Values.SelectMany(f => f.Values.SelectMany(i => i.Values)).ToList(),
            _entityRangeDictionary.Values.SelectMany(e => e.Values.SelectMany(i => i.Values)).ToList());
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TEntity), typeof(bool))));
        return Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder.FilterMethodName, Utilities.PublicStatic));
    }
}