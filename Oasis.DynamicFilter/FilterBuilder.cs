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

internal sealed class FilterConfiguration<TFilter, TEntity> : IFilterConfigurationBuilder<TFilter, TEntity>
    where TFilter : class
    where TEntity : class
{
    private static readonly Type DecimalType = typeof(decimal);
    private static readonly Type NullableDecimalType = typeof(decimal?);
    private static readonly Type FloatType = typeof(float);
    private static readonly Type NullableFloatType = typeof(float?);
    private static readonly Type DoubleType = typeof(double);
    private static readonly Type NullableDoubleType = typeof(double?);
    private readonly IFilterBuilder _builder;

    public FilterConfiguration(FilterBuilder builder)
    {
        _builder = builder;
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> FilterByProperty<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByPropertyType type,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
        Func<TFilter, bool>? ignoreIf = null,
        Func<TFilter, bool>? reverseIf = null)
    {
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

        var entityProperty = GetProperty(entityPropertyExpression);
        var filterProperty = GetProperty(filterPropertyExpression);

        throw new NotImplementedException();
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TFilter, TFilterProperty>> filterPropertyMaxExpression,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null,
        Func<TFilter, bool>? reverseIf = null)
    {
        throw new NotImplementedException();
    }

    public IFilterConfigurationBuilder<TFilter, TEntity> FilterByRange<TEntityProperty, TFilterProperty>(
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMinExpression,
        FilterByRangeType minFilteringType,
        Expression<Func<TFilter, TEntityProperty>> filterPropertyExpression,
        FilterByRangeType maxFilteringType,
        Expression<Func<TEntity, TFilterProperty>> entityPropertyMaxExpression,
        Func<TFilter, bool>? ignoreMinIf = null,
        Func<TFilter, bool>? ignoreMaxIf = null,
        Func<TFilter, bool>? reverseIf = null)
    {
        throw new NotImplementedException();
    }

    public IFilterBuilder Finish()
    {
        throw new NotImplementedException();
    }

    private static void ValidateForComparison(Type entityPropertyType, Type filterPropertyType, FilterByPropertyType type)
    {
        if (entityPropertyType == filterPropertyType)
        {
            // same type equal, allowed for primitives and enums and others with equals operator defined
            if (!entityPropertyType.IsPrimitive && !entityPropertyType.IsEnum
                && !(entityPropertyType.IsNullable(out var argumentType) && (argumentType.IsPrimitive || argumentType.IsEnum))
                && !entityPropertyType.HasOperator(type))
            {
                throw new ArgumentException($"{entityPropertyType.Name} doesn't have a defined ${type} operator.");
            }
        }
        else if (entityPropertyType.IsNullable(out var entityArgumentType) && entityArgumentType == filterPropertyType)
        {
            // nullable<T> == T case, allowed for primitive and enums
            if (!filterPropertyType.IsPrimitive && !filterPropertyType.IsEnum && !filterPropertyType.HasOperator(type))
            {
                throw new ArgumentException($"{filterPropertyType.Name} doesn't have a defined ${type} operator.");
            }
        }
        else if (filterPropertyType.IsNullable(out var filterArgumentType) && filterArgumentType == entityArgumentType)
        {
            // nullable<T> == T case, allowed for primitive and enums
            if (!entityArgumentType.IsPrimitive && !entityArgumentType.IsEnum && !entityArgumentType.HasOperator(type))
            {
                throw new ArgumentException($"{entityArgumentType.Name} doesn't have a defined ${type} operator.");
            }
        }
        else
        {
            // decimal equals to long/int/short/byte is allowed
            // primitive equals to primitive is allowed
            // the rest are not allowed
            if (!(filterPropertyType.IsPrimitive && entityPropertyType.IsPrimitive)
                && !((filterPropertyType == DecimalType || filterPropertyType == NullableDecimalType) && entityPropertyType.IsPrimitive && entityPropertyType != FloatType && entityPropertyType != DoubleType)
                && !((entityPropertyType == DecimalType || entityPropertyType == NullableDecimalType) && filterPropertyType.IsPrimitive && filterPropertyType != FloatType && filterPropertyType != DoubleType))
            {
                throw new ArgumentException($"Equality/Inequality can't be applied to {entityPropertyType.Name} and {filterPropertyType.Name}.");
            }
        }
    }

    private static void ValidateForContains(Type containerType, Type itemType)
    {

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
