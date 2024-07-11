namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class FilterTypeBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    public const string FilterMethodName = "Filter";
    private const string CompareDictionaryFieldName = "_compareDictionary";
    private const string ContainDictionaryFieldName = "_containDictionary";
    private const string InDictionaryFieldName = "_inDictionary";
    private const string CompareStringDictionaryFieldName = "_compareStringDictionary";
    private const string DictionaryItemMethodName = "get_Item";
    private static readonly MethodInfo CompareFieldGetItem = typeof(Dictionary<uint, CompareData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ContainFieldGetItem = typeof(Dictionary<uint, ContainData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo InFieldGetItem = typeof(Dictionary<uint, InData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo CompareStringFieldGetItem = typeof(Dictionary<uint, CompareStringData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly Type EntityType = typeof(TEntity);
    private static readonly Type FilterType = typeof(TFilter);
    private static readonly Type BooleanType = typeof(bool);
    private static readonly ConstructorInfo ParameterReplacerConstructor = typeof(ExpressionParameterReplacer).GetConstructor(new[] { typeof(ParameterExpression) });
    private static readonly Type ParameterExpressionType = typeof(ParameterExpression);
    private static readonly MethodInfo TypeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), Utilities.PublicStatic)!;
    private static readonly MethodInfo ParameterMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == nameof(Expression.Parameter) && m.GetParameters().Length == 2)!;
    private static readonly MethodInfo LambdaMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == "Lambda" && m.GetParameters().Length == 2);
    private static readonly MethodInfo ConstantExpressionMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == "Constant" && m.GetParameters().Length == 1);
    private readonly ModuleBuilder _moduleBuilder;

    public FilterTypeBuilder(ModuleBuilder moduleBuilder)
    {
        _moduleBuilder = moduleBuilder;
    }

    internal Type? Build(ISet<string>? configuredEntityProperties = null)
    {
        uint fieldIndex = 0;
        var (compare, compareString, contain, isIn) = ExtractFilterProperties(fieldIndex, configuredEntityProperties);

        var filterConditionCount = GetFilteringConditionCount(compare, compareString, contain, isIn);
        if (filterConditionCount > 0)
        {
            var entityType = typeof(TEntity);
            var filterType = typeof(TFilter);
            var typeBuilder = _moduleBuilder.DefineType(GetDynamicTypeName(entityType, filterType), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);
            var methodBuilder = typeBuilder.DefineMethod(FilterMethodName, MethodAttributes.Public | MethodAttributes.Static);
            methodBuilder.SetParameters(filterType);
            methodBuilder.SetReturnType(typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
            var generator = methodBuilder.GetILGenerator();

            // generate starting code
            var expressionLocal = generator.DeclareLocal(typeof(Expression));
            _ = generator.DeclareLocal(ParameterExpressionType);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldtoken, EntityType);
            generator.Emit(OpCodes.Call, TypeOfMethod);
            generator.Emit(OpCodes.Ldstr, "t");
            generator.Emit(OpCodes.Call, ParameterMethod);
            generator.Emit(OpCodes.Stloc_1);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Newobj, ParameterReplacerConstructor);

            Dictionary<uint, CompareData<TFilter>>? compareData = default;
            if (compare.Any())
            {
                var buildCompareExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCompareExpression), Utilities.PublicStatic);
                var compareDictionaryField = typeBuilder.DefineField(CompareDictionaryFieldName, typeof(Dictionary<uint, CompareData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in compare)
                {
                    void CallBuildExpressionMethod() => generator.Emit(OpCodes.Call, buildCompareExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                    GenerateFieldFilterCode(generator, compareDictionaryField, c.id, CompareFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                compareData = compare.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, CompareStringData<TFilter>>? compareStringData = default;
            if (compareString.Any())
            {
                var buildCompareStringExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildStringCompareExpression), Utilities.PublicStatic);
                var compareStringDictionaryField = typeBuilder.DefineField(CompareStringDictionaryFieldName, typeof(Dictionary<uint, CompareStringData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in compareString)
                {
                    void CallBuildExpressionMethod() => generator.Emit(OpCodes.Call, buildCompareStringExpressionMethod.MakeGenericMethod(FilterType));
                    GenerateFieldFilterCode(generator, compareStringDictionaryField, c.id, CompareStringFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                compareStringData = compareString.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, ContainData<TFilter>>? containData = default;
            if (contain.Any())
            {
                var buildCollectionContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCollectionContainsExpression), Utilities.PublicStatic);
                var buildArrayContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildArrayContainsExpression), Utilities.PublicStatic);
                var containDictionaryField = typeBuilder.DefineField(ContainDictionaryFieldName, typeof(Dictionary<uint, ContainData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in contain)
                {
                    void CallBuildExpressionMethod()
                    {
                        if (c.isCollection)
                        {
                            generator.Emit(OpCodes.Call, buildCollectionContainExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                        }
                        else
                        {
                            generator.Emit(OpCodes.Call, buildArrayContainExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                        }
                    }

                    GenerateFieldFilterCode(generator, containDictionaryField, c.id, ContainFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                containData = contain.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, InData<TFilter>>? inData = default;
            if (isIn.Any())
            {
                var buildInCollectionExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInCollectionExpression), Utilities.PublicStatic);
                var buildInArrayExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInArrayExpression), Utilities.PublicStatic);
                var inDictionaryField = typeBuilder.DefineField(InDictionaryFieldName, typeof(Dictionary<uint, InData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var i in isIn)
                {
                    void CallBuildExpressionMethod()
                    {
                        if (i.isCollection)
                        {
                            generator.Emit(OpCodes.Call, buildInCollectionExpressionMethod.MakeGenericMethod(FilterType, i.filterPropertyType));
                        }
                        else
                        {
                            generator.Emit(OpCodes.Call, buildInArrayExpressionMethod.MakeGenericMethod(FilterType, i.filterPropertyType));
                        }
                    }

                    GenerateFieldFilterCode(generator, inDictionaryField, i.id, InFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                inData = isIn.ToDictionary(i => i.id, i => i);
            }

            // Generate ending code
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Dup);
            var parameterJump = generator.DefineLabel();
            generator.Emit(OpCodes.Brtrue_S, parameterJump);
            generator.Emit(OpCodes.Pop);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Box, BooleanType);
            generator.Emit(OpCodes.Call, ConstantExpressionMethod);
            generator.MarkLabel(parameterJump);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Newarr, ParameterExpressionType);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Stelem_Ref);
            generator.Emit(OpCodes.Call, LambdaMethod.MakeGenericMethod(typeof(Func<,>).MakeGenericType(EntityType, BooleanType)));
            generator.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType()!;

            if (compareData != null)
            {
                WrapUpFilterCode(type, CompareDictionaryFieldName, compareData);
            }

            if (compareStringData != null)
            {
                WrapUpFilterCode(type, CompareStringDictionaryFieldName, compareStringData);
            }

            if (containData != null)
            {
                WrapUpFilterCode(type, ContainDictionaryFieldName, containData);
            }

            if (inData != null)
            {
                WrapUpFilterCode(type, InDictionaryFieldName, inData);
            }

            return type;
        }

        return null;
    }

    private static int GetFilteringConditionCount(
        List<CompareData<TFilter>> compareList,
        List<CompareStringData<TFilter>> compareStringList,
        List<ContainData<TFilter>> containList,
        List<InData<TFilter>> inList)
        => compareList.Count + compareStringList.Count + containList.Count + inList.Count;

    private static void WrapUpFilterCode<TData>(Type type, string fieldName, Dictionary<uint, TData> data)
        => type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, data);

    private static Func<TFilter, bool> BuildFilterPropertyIsDefaultFunction(PropertyInfo filterProperty)
    {
        var parameter = Expression.Parameter(typeof(TFilter), "t");
        return Expression.Lambda<Func<TFilter, bool>>(Expression.Equal(Expression.Default(filterProperty.PropertyType), Expression.Property(parameter, filterProperty)), parameter).Compile();
    }

    private static void LoadUint(ILGenerator generator, uint number)
    {
        if (number == 0)
        {
            generator.Emit(OpCodes.Ldc_I4_0);
        }
        else if (number == 1)
        {
            generator.Emit(OpCodes.Ldc_I4_1);
        }
        else if (number == 2)
        {
            generator.Emit(OpCodes.Ldc_I4_2);
        }
        else if (number == 3)
        {
            generator.Emit(OpCodes.Ldc_I4_3);
        }
        else if (number == 4)
        {
            generator.Emit(OpCodes.Ldc_I4_4);
        }
        else if (number == 5)
        {
            generator.Emit(OpCodes.Ldc_I4_5);
        }
        else if (number == 6)
        {
            generator.Emit(OpCodes.Ldc_I4_6);
        }
        else if (number == 7)
        {
            generator.Emit(OpCodes.Ldc_I4_7);
        }
        else if (number == 8)
        {
            generator.Emit(OpCodes.Ldc_I4_8);
        }
        else if (number <= 127)
        {
            generator.Emit(OpCodes.Ldc_I4_S, number);
        }
        else
        {
            generator.Emit(OpCodes.Ldc_I4, number);
        }
    }

    private static string GetDynamicTypeName(Type entityType, Type filterType) => $"Filter_{entityType.Name}_{filterType.Name}_{Utilities.GenerateRandomName(16)}";

    private (List<CompareData<TFilter>>, List<CompareStringData<TFilter>>, List<ContainData<TFilter>>, List<InData<TFilter>>) ExtractFilterProperties(uint fieldIndex, ISet<string>? configuredEntityProperties)
    {
        var filterProperties = typeof(TFilter).GetProperties(Utilities.PublicInstance).Where(p => p.GetMethod != default);
        var entityProperties = typeof(TEntity).GetProperties(Utilities.PublicInstance)
            .Where(p => p.GetMethod != default && (configuredEntityProperties == null || !configuredEntityProperties.Contains(p.Name)))
            .ToDictionary(p => p.Name, p => p);

        var compareList = new List<CompareData<TFilter>>();
        var containList = new List<ContainData<TFilter>>();
        var inList = new List<InData<TFilter>>();
        var compareStringList = new List<CompareStringData<TFilter>>();
        foreach (var filterProperty in filterProperties)
        {
            if (entityProperties.TryGetValue(filterProperty.Name, out var entityProperty))
            {
                var param = Expression.Parameter(typeof(TFilter));
                var filterPropertyFunc = Expression.Lambda(Expression.Property(param, filterProperty), param).Compile();
                var filterPropertyType = filterProperty.PropertyType;
                var entityPropertyType = entityProperty.PropertyType;
                var parameter = Expression.Parameter(typeof(TEntity));
                var entityPropertyExpression = Expression.Lambda(Expression.Property(parameter, entityProperty), parameter);
                if (entityPropertyType == typeof(string) && filterPropertyType == typeof(string))
                {
                    var ignoreIf = BuildFilterPropertyIsDefaultFunction(filterProperty);
                    compareStringList.Add(new CompareStringData<TFilter>(fieldIndex++, entityPropertyExpression, filterPropertyFunc, ignoreIf));
                    continue;
                }

                var comparison = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType);
                if (comparison != null)
                {
                    var c = comparison.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? BuildFilterPropertyIsDefaultFunction(filterProperty) : null;
                    compareList.Add(new CompareData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, c.leftConvertTo, filterPropertyFunc, filterPropertyType, c.rightConvertTo, ignoreIf));
                    continue;
                }

                var contains = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (contains != null)
                {
                    var value = contains.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? BuildFilterPropertyIsDefaultFunction(filterProperty) : null;
                    containList.Add(new ContainData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, value.containerItemType, filterPropertyFunc, filterPropertyType, value.itemConvertTo, value.isCollection, value.nullValueNotCovered, ignoreIf));
                    continue;
                }

                var isIn = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (isIn != null)
                {
                    var value = isIn.Value;
                    inList.Add(new InData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, value.itemConvertTo, filterPropertyFunc, filterPropertyType, isIn.Value.containerItemType, value.isCollection, value.nullValueNotCovered));
                    continue;
                }
            }
        }

        return (compareList, compareStringList, containList, inList);
    }

    private void GenerateFieldFilterCode(
        ILGenerator generator,
        FieldInfo dataDictionaryField,
        uint id,
        MethodInfo getItem,
        LocalBuilder expressionLocal,
        Action callBuildExpressionMethod,
        ref int filterConditionCount)
    {
        if (filterConditionCount > 1)
        {
            generator.Emit(OpCodes.Dup);
        }

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldsfld, dataDictionaryField);
        LoadUint(generator, id);
        generator.Emit(OpCodes.Callvirt, getItem);
        generator.Emit(OpCodes.Ldloca_S, expressionLocal);
        callBuildExpressionMethod();
        filterConditionCount--;
    }
}

public record struct CompareData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type? entityPropertyConvertTo,
    Delegate filterFunc,
    Type filterPropertyType,
    Type? filterPropertyConvertTo,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct ContainData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type entityPropertyItemType,
    Delegate filterFunc,
    Type filterPropertyType,
    Type? filterPropertyConvertTo,
    bool isCollection,
    bool nullValueNotCovered,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct InData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type? entityPropertyConvertTo,
    Delegate filterFunc,
    Type filterPropertyType,
    Type filterPropertyItemType,
    bool isCollection,
    bool nullValueNotCovered)
    where TFilter : class;

public record struct CompareStringData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Delegate filterFunc,
    Func<TFilter, bool>? ignore)
    where TFilter : class;
