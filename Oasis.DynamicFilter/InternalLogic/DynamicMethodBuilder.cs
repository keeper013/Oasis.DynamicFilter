namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class DynamicMethodBuilder
{
    private readonly TypeBuilder _typeBuilder;

    public DynamicMethodBuilder(TypeBuilder typeBuilder)
    {
        _typeBuilder = typeBuilder;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData BuildUpFilterMethod(Type filterType, Type entityType, IFilterGlobalConfiguration? globalConfiguration, IFilterConfiguration? configuration = null)
    {
        var filterProperties = filterType.GetProperties(Utilities.PublicInstance).ToList();
        var entityProperties = entityType.GetProperties(Utilities.PublicInstance).ToList();
        var matchedScalarProperties = ExtractScalarProperties(filterProperties, entityProperties);
        if (!matchedScalarProperties.Any())
        {
            throw new BadFilterException(filterType, entityType);
        }

        var methodName = BuildMethodName(filterType, entityType);
        var method = BuildMethod(methodName, new[] { filterType }, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        var generator = method.GetILGenerator();
        GenerateKeyPropertiesMappingCode(generator, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.GetExpression<,>).MakeGenericType(filterType, entityType), method.Name);
    }

    private static string BuildMethodName(Type sourceType, Type targetType)
    {
        return $"_Filter__{GetTypeName(sourceType)}__Entity__{GetTypeName(targetType)}";
    }

    private static string GetTypeName(Type type)
    {
        return $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
    }

    private MethodBuilder BuildMethod(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
    }

    private IList<(PropertyInfo, PropertyInfo)> ExtractScalarProperties(IList<PropertyInfo> filterProperties, IList<PropertyInfo> entityProperties)
    {
        var filterScalarProperties = filterProperties.Where(p => p.PropertyType.IsScalarType());
        var entityScalarProperties = entityProperties.Where(p => p.PropertyType.IsScalarType()).ToDictionary(p => p.Name, p => p);

        var matchedProperties = new List<(PropertyInfo, PropertyInfo)>(Math.Min(filterScalarProperties.Count(), entityScalarProperties.Count()));
        foreach (var filterProperty in filterScalarProperties)
        {
            if (entityScalarProperties.TryGetValue(filterProperty.Name, out var entityProperty) && filterProperty.PropertyType.Matches(entityProperty.PropertyType))
            {
                matchedProperties.Add((filterProperty, entityProperty));
            }
        }

        foreach (var match in matchedProperties)
        {
            filterProperties.Remove(match.Item1);
            entityProperties.Remove(match.Item2);
        }

        return matchedProperties;
    }
}
