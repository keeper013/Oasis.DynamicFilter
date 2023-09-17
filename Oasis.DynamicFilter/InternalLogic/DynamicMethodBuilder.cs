namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class GlobalFilterPropertyManager : IFilterPropertyManager
{
    private IGlobalExcludedPropertyManager? _globalFilterPropertyManager = null!;
    private Type _filterType = null!;
    private Type _entityType = null!;

    public void Set(IGlobalExcludedPropertyManager? globalExcludedPropertyManager, Type filterType, Type entityType)
    {
        _globalFilterPropertyManager = globalExcludedPropertyManager;
        _filterType = filterType;
        _entityType = entityType;
    }

    public bool IsEntityPropertyExcluded(string propertyName)
    {
        return _globalFilterPropertyManager != null && _globalFilterPropertyManager.IsEntityPropertyExcluded(_entityType, propertyName);
    }

    public bool IsFilterPropertyExcluded(string propertyName)
    {
        return _globalFilterPropertyManager != null && _globalFilterPropertyManager.IsFilterPropertyExcluded(_filterType, propertyName);
    }

    public (string, FilteringType) GetFilteringType(string propertyName)
    {
        return (propertyName, FilteringType.Default);
    }
}

internal sealed class DynamicMethodBuilder : IEqualityMethodBuilder
{
    private readonly TypeBuilder _typeBuilder;
    private readonly GlobalFilterPropertyManager _globalExcludedPropertyManager;

    public DynamicMethodBuilder(TypeBuilder typeBuilder)
    {
        _typeBuilder = typeBuilder;
        _globalExcludedPropertyManager = new GlobalFilterPropertyManager();
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData BuildUpFilterMethod(Type filterType, Type entityType, IGlobalExcludedPropertyManager? globalConfiguration = null)
    {
        _globalExcludedPropertyManager.Set(globalConfiguration, filterType, entityType);
        return BuildUpFilterMethod(filterType, entityType, _globalExcludedPropertyManager);
    }

    public MethodMetaData BuildUpFilterMethod(Type filterType, Type entityType, IFilterPropertyManager manager)
    {
        var filterProperties = filterType.GetProperties(Utilities.PublicInstance).Where(p => !manager.IsFilterPropertyExcluded(p.Name)).ToList();
        var entityProperties = entityType.GetProperties(Utilities.PublicInstance).Where(p => !manager.IsEntityPropertyExcluded(p.Name)).ToList();
        var matchedScalarProperties = ExtractScalarProperties(filterProperties, entityProperties, manager);
        if (!matchedScalarProperties.Any())
        {
            throw new BadFilterException(filterType, entityType);
        }

        var methodName = BuildMethodName(filterType, entityType);
        var method = BuildMethod(methodName, new[] { filterType, typeof(IFilterPropertyExcludeByValueManager) }, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        var generator = method.GetILGenerator();
        GenerateKeyPropertiesMappingCode(generator, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.GetExpression<,>).MakeGenericType(filterType, entityType), method.Name);
    }

    public MethodMetaData BuildEqualityMethod(Type type)
    {
        throw new NotImplementedException();
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

    private IList<(PropertyInfo, PropertyInfo, FilteringType)> ExtractScalarProperties(IList<PropertyInfo> filterProperties, IList<PropertyInfo> entityProperties, IFilterPropertyManager filterPropertyManager)
    {
        var filterScalarProperties = filterProperties.Where(p => p.PropertyType.IsScalarType());
        var entityScalarProperties = entityProperties.Where(p => p.PropertyType.IsScalarType()).ToDictionary(p => p.Name, p => p);

        var matchedProperties = new List<(PropertyInfo, PropertyInfo, FilteringType)>(Math.Min(filterScalarProperties.Count(), entityScalarProperties.Count()));
        foreach (var filterProperty in filterScalarProperties)
        {
            var filterTuple = filterPropertyManager.GetFilteringType(filterProperty.Name);
            if (entityScalarProperties.TryGetValue(filterTuple.Item1, out var entityProperty) && filterProperty.PropertyType.Matches(entityProperty.PropertyType))
            {
                matchedProperties.Add((filterProperty, entityProperty, filterTuple.Item2));
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
