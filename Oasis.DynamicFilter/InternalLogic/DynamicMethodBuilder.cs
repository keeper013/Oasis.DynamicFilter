namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter;
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
}

internal interface IEqualityMethodBuilder
{
    MethodMetaData BuildEqualityMethod(Type type);
}

internal enum TypeEqualCategory
{
    /// <summary>
    /// Types overrides equal operator
    /// </summary>
    OpEquality,

    /// <summary>
    /// Types that are primitive or enum or class
    /// </summary>
    PrimitiveEnumClass,

    /// <summary>
    /// Nullable structs that overrides equal operator
    /// </summary>
    NullableOpEquality,

    /// <summary>
    /// Nullable primitive or enum
    /// </summary>
    NullablePrimitiveEnum,

    /// <summary>
    /// Equals as objects
    /// </summary>
    ObjectEquals,
}

internal sealed class DynamicMethodBuilder : IEqualityMethodBuilder
{
    private const string GetValueOrDefaultMethodName = "GetValueOrDefault";
    private const string HasValuePropertyName = "HasValue";
    private static readonly Type BoolType = typeof(bool);
    private static readonly MethodInfo ObjectEqualsMethod = typeof(object).GetMethod(nameof(object.Equals), Utilities.PublicStatic)!;
    private readonly TypeBuilder _typeBuilder;
    private readonly GlobalFilterPropertyManager _globalExcludedPropertyManager;
    private readonly HashSet<Type> _convertableToScalarTypes;
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary;

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

        var methodName = BuildFilterMethodName(filterType, entityType);
        var method = BuildMethod(methodName, new[] { filterType, typeof(IFilterPropertyExcludeByValueManager) }, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        var generator = method.GetILGenerator();
        GenerateKeyPropertiesMappingCode(generator, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.GetExpression<,>).MakeGenericType(filterType, entityType), method.Name);
    }

    public MethodMetaData BuildEqualityMethod(Type type)
    {
        var methodName = BuildEqualMethodName(type);
        var method = BuildMethod(methodName, new[] { type, type }, BoolType);
        var generator = method.GetILGenerator();

        var equalCategory = GetTypeEqualityCategory(type);
        var typeIsNullable = equalCategory == TypeEqualCategory.NullableOpEquality || equalCategory == TypeEqualCategory.NullablePrimitiveEnum;
        var needToBox = equalCategory == TypeEqualCategory.ObjectEquals && type.IsValueType;
        LocalBuilder? sourceLocal = null;
        LocalBuilder? targetLocal = null;
        if (typeIsNullable)
        {
            sourceLocal = generator.DeclareLocal(type);
            targetLocal = generator.DeclareLocal(type);
        }

        generator.Emit(OpCodes.Ldarg_0);
        if (typeIsNullable)
        {
            generator.Emit(OpCodes.Stloc_0);
        }

        if (needToBox)
        {
            generator.Emit(OpCodes.Box, type);
        }

        generator.Emit(OpCodes.Ldarg_1);
        if (typeIsNullable)
        {
            generator.Emit(OpCodes.Stloc_1);
        }

        if (needToBox)
        {
            generator.Emit(OpCodes.Box, type);
        }

        GenerateEqualCode(generator, type, equalCategory, sourceLocal, targetLocal);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Func<,,>).MakeGenericType(type, type, BoolType), methodName);
    }

    private static void GenerateEqualCode(ILGenerator generator, Type type, TypeEqualCategory category, LocalBuilder? sourceLocal, LocalBuilder? targetLocal)
    {
        if (sourceLocal != default)
        {
            // nullable value type case
            var getValueOrDefaultMethod = type.GetMethod(GetValueOrDefaultMethodName, Array.Empty<Type>())!;
            var hasValueGetter = type.GetProperty(HasValuePropertyName, Utilities.PublicInstance)!.GetGetMethod()!;
            generator.Emit(OpCodes.Ldloca_S, sourceLocal);
            generator.Emit(OpCodes.Call, getValueOrDefaultMethod);
            generator.Emit(OpCodes.Ldloca_S, targetLocal!);
            generator.Emit(OpCodes.Call, getValueOrDefaultMethod);
            if (category == TypeEqualCategory.NullableOpEquality)
            {
                generator.Emit(OpCodes.Call, type.GenericTypeArguments[0].GetMethod(Utilities.EqualOperatorMethodName, Utilities.PublicStatic)!);
            }
            else
            {
                generator.Emit(OpCodes.Ceq);
            }

            generator.Emit(OpCodes.Ldloca_S, sourceLocal);
            generator.Emit(OpCodes.Call, hasValueGetter);
            generator.Emit(OpCodes.Ldloca_S, targetLocal!);
            generator.Emit(OpCodes.Call, hasValueGetter);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.And);
        }
        else if (category == TypeEqualCategory.OpEquality)
        {
            generator.Emit(OpCodes.Call, type.GetMethod(Utilities.EqualOperatorMethodName, Utilities.PublicStatic)!);
        }
        else if (category == TypeEqualCategory.PrimitiveEnumClass)
        {
            generator.Emit(OpCodes.Ceq);
        }
        else
        {
            generator.Emit(OpCodes.Call, ObjectEqualsMethod);
        }
    }

    private static TypeEqualCategory GetTypeEqualityCategory(Type type)
    {
        var equalOperator = type.GetMethod(Utilities.EqualOperatorMethodName, Utilities.PublicStatic);
        if (equalOperator != null && equalOperator.IsHideBySig && equalOperator.IsSpecialName)
        {
            return TypeEqualCategory.OpEquality;
        }

        if (type.IsPrimitive || type.IsEnum || type.IsClass)
        {
            return TypeEqualCategory.PrimitiveEnumClass;
        }

        if (type.IsNullable(out var argumentType))
        {
            equalOperator = argumentType.GetMethod(Utilities.EqualOperatorMethodName, Utilities.PublicStatic);
            if (equalOperator != null && equalOperator.IsHideBySig && equalOperator.IsSpecialName)
            {
                return TypeEqualCategory.NullableOpEquality;
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return TypeEqualCategory.NullablePrimitiveEnum;
            }
        }

        return TypeEqualCategory.ObjectEquals;
    }

    private static string BuildFilterMethodName(Type sourceType, Type targetType)
    {
        return $"_Filter_{GetTypeName(sourceType)}__Entity__{GetTypeName(targetType)}";
    }

    private static string BuildEqualMethodName(Type propertyType)
    {
        return $"_Equal_{GetTypeName(propertyType)}_";
    }

    private static string GetTypeName(Type type)
    {
        return $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
    }

    private IList<(PropertyInfo, PropertyInfo)> ExtractScalarProperties(IList<PropertyInfo> filterProperties, IList<PropertyInfo> entityProperties, IFilterPropertyManager manager)
    {
        var filterScalarProperties = filterProperties.Where(p => (p.PropertyType.IsFilterableScalarType() || _convertableToScalarTypes.Contains(p.PropertyType)) && p.GetMethod != default);
        var entityScalarProperties = entityProperties.Where(p => (p.PropertyType.IsFilterableScalarType() || _convertableToScalarTypes.Contains(p.PropertyType)) && p.GetMethod != default).ToDictionary(p => p.Name, p => p);

        var matchedProperties = new List<(PropertyInfo, PropertyInfo)>(Math.Min(filterScalarProperties.Count(), entityScalarProperties.Count()));
        foreach (var filterProperty in filterScalarProperties)
        {
            var filterPropertyType = filterProperty.PropertyType;
            if (entityScalarProperties.TryGetValue(filterProperty.Name, out var entityProperty)
                && (filterPropertyType == entityProperty.PropertyType
                    || filterPropertyType.GetListItemType() == entityProperty.PropertyType
                    || _scalarConverterDictionary.Contains(filterPropertyType, entityProperty.PropertyType)))
            {
                matchedProperties.Add((filterProperty, entityProperty));
            }
        }

        return matchedProperties;
    }

    private MethodBuilder BuildMethod(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
    }
}
