namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.Exceptions;
using Oasis.DynamicFilter.InternalLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

internal record struct ByPropertyFilter<TFilter>(
    PropertyInfo entityProperty,
    FilterByPropertyType type,
    PropertyInfo filterProperty,
    Func<TFilter, bool>? revertIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct ByFilterRangeFilter<TFilter>(
    PropertyInfo minFilterProperty,
    FilterByPropertyType minFilterType,
    PropertyInfo entityProperty,
    FilterByPropertyType maxFilterType,
    PropertyInfo maxFilterProperty,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreMinIf,
    Func<TFilter, bool>? ignoreMaxIf)
    where TFilter : class;

internal record struct ByEntityRangeFilter<TFilter>(
    PropertyInfo minEntityProperty,
    FilterByPropertyType minEntityType,
    PropertyInfo filterProperty,
    FilterByPropertyType maxEntityType,
    PropertyInfo maxEntityProperty,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal sealed class FilterConfiguration<TEntity, TFilter> : IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private static readonly Type DecimalType = typeof(decimal);
    private static readonly Type FloatType = typeof(float);
    private static readonly Type DoubleType = typeof(double);
    private static readonly Type StringType = typeof(string);
    private static readonly IReadOnlyDictionary<Type, int> NumericSizeDictionary = new Dictionary<Type, int>
    {
        { typeof(sbyte), sizeof(sbyte) },
        { typeof(byte), sizeof(byte) },
        { typeof(short), sizeof(short) },
        { typeof(ushort), sizeof(ushort) },
        { typeof(int), sizeof(int) },
        { typeof(uint), sizeof(uint) },
        { typeof(long), sizeof(long) },
        { typeof(ulong), sizeof(ulong) },
    };

    private readonly IFilterBuilder _builder;
    private readonly Dictionary<string, Dictionary<string, ByPropertyFilter<TFilter>>> _byPropertyFilters = new ();
    private readonly Dictionary<string, Dictionary<string, ByFilterRangeFilter<TFilter>>> _byFilterRangeFilters = new ();
    private readonly Dictionary<string, Dictionary<string, ByEntityRangeFilter<TFilter>>> _byEntityRangeFilters = new ();

    public FilterConfiguration(FilterBuilder builder)
    {
        _builder = builder;
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

        if (_byPropertyFilters.Contains(entityProperty.Name, filterProperty.Name))
        {
            throw new RedundantMatchingException(typeof(TEntity), entityProperty.Name, typeof(TFilter), filterProperty.Name);
        }

        var entityPropertyType = typeof(TEntityProperty);
        var filterPropertyType = typeof(TFilterProperty);
        switch (type)
        {
            case FilterByPropertyType.In:
            case FilterByPropertyType.NotIn:
                ValidateForContains(filterPropertyType, entityPropertyType);
                break;
            case FilterByPropertyType.Contains:
            case FilterByPropertyType.NotContains:
                ValidateForContains(entityPropertyType, filterPropertyType);
                break;
            default:
                ValidateForComparison(entityPropertyType, filterPropertyType, type);
                break;
        }

        if ((filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)) && ignoreIf is null)
        {
            ignoreIf = BuildFilterPropertyIsDefaultFunction(filterProperty).Compile();
        }

        _byPropertyFilters.Add(entityProperty.Name, filterProperty.Name, new ByPropertyFilter<TFilter>(entityProperty, type, filterProperty, reverseIf, ignoreIf));

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMaxExpression,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null)
    {
        throw new NotImplementedException();
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TFilter, TEntityProperty>> filterPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? reverseIf = null,
        Func<TFilter, bool>? ignoreIf = null)
    {
        throw new NotImplementedException();
    }

    public IFilterBuilder Finish()
    {
        throw new NotImplementedException();
    }

    private static Expression<Func<TFilter, bool>> BuildFilterPropertyIsDefaultFunction(PropertyInfo filterProperty)
    {
        var parameter = Expression.Parameter(typeof(TFilter), "t");
        return Expression.Lambda<Func<TFilter, bool>>(Expression.Equal(Expression.Default(filterProperty.PropertyType), Expression.Property(parameter, filterProperty)), parameter);
    }

    private static void ValidateForComparison(Type entityPropertyType, Type filterPropertyType, FilterByPropertyType type)
    {
        entityPropertyType = entityPropertyType.IsNullable(out var entityArgumentType) ? entityArgumentType : entityPropertyType;
        filterPropertyType = filterPropertyType.IsNullable(out var filterArgumentType) ? filterArgumentType : filterPropertyType;
        if (entityPropertyType == filterPropertyType)
        {
            // same type comparison, allowed for primitives and enums and others with compare operator defined
            if (!entityPropertyType.IsPrimitive && !entityPropertyType.IsEnum && !filterPropertyType.IsPrimitive && !filterPropertyType.IsEnum && !entityPropertyType.HasOperator(type))
            {
                throw new ArgumentException($"{entityPropertyType.Name} doesn't have a defined ${type} operator.");
            }
        }
        else if (!(filterPropertyType.IsPrimitive && entityPropertyType.IsPrimitive)
            && !(filterPropertyType == DecimalType && entityPropertyType.IsPrimitive && entityPropertyType != FloatType && entityPropertyType != DoubleType)
            && !(entityPropertyType == DecimalType && filterPropertyType.IsPrimitive && filterPropertyType != FloatType && filterPropertyType != DoubleType))
        {
            // decimal compare to long/int/short/byte is allowed
            // primitive compare to primitive is allowed
            // the rest are not allowed
            throw new ArgumentException($"Equality/Inequality can't be applied to {entityPropertyType.Name} and {filterPropertyType.Name}.");
        }
    }

    private static void ValidateForContains(Type containerType, Type itemType)
    {
        var containerItemType = containerType.GetCollectionItemType();
        if (containerItemType == null && containerType.IsArray)
        {
            containerItemType = containerType.GetElementType();
        }

        if (containerItemType != null)
        {
            // type collection contains equal type is allowed for value types and string (for class types contains match them by reference)
            if (containerItemType == itemType)
            {
                if (itemType == StringType || (itemType.IsValueType && (itemType.IsPrimitive || itemType.IsEnum || itemType.HasOperator(FilterByPropertyType.Equality))))
                {
                    return;
                }
            }
            else if (containerItemType.IsNullable(out var containerItemArgumentType))
            {
                if (NonNullableValueTypeCanContain(containerItemArgumentType, itemType.IsNullable(out var itemargumentType) ? itemargumentType : itemType))
                {
                    return;
                }
            }
            else
            {
                if (NonNullableValueTypeCanContain(containerItemType, itemType))
                {
                    return;
                }
            }
        }

        throw new ArgumentException($"{containerType.Name} can't be the container type for {itemType.Name}.");
    }

    /// <summary>
    /// This is only for different value types containing judgement
    /// double type can contain any primitive.
    /// float type can contain any primitive but double.
    /// decimal type can contain any primitive but double and float.
    /// other primitive types must contain shorter primitive types.
    /// </summary>
    /// <param name="containerItemType">Container item type.</param>
    /// <param name="itemType">Item to be contained.</param>
    /// <returns>True if can contain, else false.</returns>
    private static bool NonNullableValueTypeCanContain(Type containerItemType, Type itemType)
        => (containerItemType == DoubleType && itemType.IsPrimitive)
            || (containerItemType == FloatType && itemType.IsPrimitive && itemType != DoubleType)
            || (containerItemType == DecimalType && itemType.IsPrimitive && itemType != DoubleType && itemType != FloatType)
            || (containerItemType.IsPrimitive && containerItemType != DoubleType && containerItemType != FloatType && itemType.IsPrimitive &&
                itemType != DoubleType && itemType != FloatType && NumericSizeDictionary[containerItemType] > NumericSizeDictionary[itemType]);

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
}

public sealed class FilterBuilder : IFilterBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;

    public FilterBuilder()
    {
        var name = new AssemblyName($"{GenerateRandomTypeName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new (module.DefineType("Mapper", TypeAttributes.Public));
    }

    public IFilter Build()
    {
        throw new NotImplementedException();
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        throw new NotImplementedException();
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
