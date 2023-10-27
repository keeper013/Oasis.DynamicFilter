namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

internal sealed class FilterTypeBuilder
{
    public const string FilterMethodName = "Filter";
    private readonly ModuleBuilder _moduleBuilder;
    private readonly StringOperator _defaultStringOperator;

    public FilterTypeBuilder(ModuleBuilder moduleBuilder, StringOperator defaultStringOperator)
    {
        _moduleBuilder = moduleBuilder;
        _defaultStringOperator = defaultStringOperator;
    }

    public FilterMethodBuilder<TEntity, TFilter> BuildFilterMethodBuilder<TEntity, TFilter>(StringOperator? defaultStringOperator)
        where TEntity : class
        where TFilter : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        var typeBuilder = _moduleBuilder.DefineType(GetDynamicTypeName(entityType, filterType), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);
        var methodBuilder = typeBuilder.DefineMethod(FilterMethodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(filterType);
        methodBuilder.SetReturnType(typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        var generator = methodBuilder.GetILGenerator();
        return new FilterMethodBuilder<TEntity, TFilter>(typeBuilder, generator, defaultStringOperator ?? _defaultStringOperator);
    }

    private static string GetDynamicTypeName(Type entityType, Type filterType) => $"Filter_{entityType.Name}_{filterType.Name}_{Utilities.GenerateRandomName(16)}";
}

public record struct CompareData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type? entityPropertyConvertTo,
    Operator op,
    Delegate filterFunc,
    Type filterPropertyType,
    Type? filterPropertyConvertTo,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct ContainData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type entityPropertyItemType,
    Operator op,
    Delegate filterFunc,
    Type filterPropertyType,
    Type? filterPropertyConvertTo,
    bool isCollection,
    bool nullValueNotCovered,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct InData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type? entityPropertyConvertTo,
    Operator op,
    Delegate filterFunc,
    Type filterPropertyType,
    Type filterPropertyItemType,
    bool isCollection,
    bool nullValueNotCovered,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct CompareStringData<TFilter>(
    uint id,
    LambdaExpression entityPropertyExpression,
    StringOperator op,
    Delegate filterFunc,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

public record struct FilterRangeData<TFilter>(
    uint id,
    Delegate filterMinFunc,
    Type filterMinPropertyType,
    Type? filterMinPropertyConvertTo,
    RangeOperator minOp,
    Type? entityMinPropertyConvertTo,
    LambdaExpression entityPropertyExpression,
    Type entityPropertyType,
    Type? entityMaxPropertyConvertTo,
    RangeOperator maxOp,
    Type? filterMaxPropertyConvertTo,
    Delegate filterMaxFunc,
    Type filterMaxPropertyType,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignoreMin,
    Func<TFilter, bool>? ignoreMax)
    where TFilter : class;

public record struct EntityRangeData<TFilter>(
    uint id,
    LambdaExpression entityMinPropertyExpression,
    Type entityMinPropertyType,
    Type? entityMinPropertyConvertTo,
    RangeOperator minOp,
    Type? filterMinPropertyConvertTo,
    Delegate filterFunc,
    Type filterPropertyType,
    Type? filterMaxPropertyConvertTo,
    RangeOperator maxOp,
    Type? entityMaxPropertyConvertTo,
    LambdaExpression entityMaxPropertyExpression,
    Type entityMaxPropertyType,
    Func<TFilter, bool>? includeNullMin,
    Func<TFilter, bool>? includeNullMax,
    Func<TFilter, bool>? reverse,
    Func<TFilter, bool>? ignore)
    where TFilter : class;

internal sealed class FilterMethodBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private const string CompareDictionaryFieldName = "_compareDictionary";
    private const string ContainDictionaryFieldName = "_containDictionary";
    private const string InDictionaryFieldName = "_inDictionary";
    private const string CompareStringDictionaryFieldName = "_compareStringDictionary";
    private const string FilterRangeDictionaryFieldName = "_filterRangeDictionary";
    private const string EntityRangeDictionaryFieldName = "_entityRangeDictionary";
    private const string DictionaryItemMethodName = "get_Item";
    private static readonly MethodInfo CompareFieldGetItem = typeof(Dictionary<uint, CompareData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ContainFieldGetItem = typeof(Dictionary<uint, ContainData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo InFieldGetItem = typeof(Dictionary<uint, InData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo FilterRangeFieldGetItem = typeof(Dictionary<uint, FilterRangeData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo EntityRangeFieldGetItem = typeof(Dictionary<uint, EntityRangeData<TFilter>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
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
    private readonly TypeBuilder _typeBuilder;
    private readonly ILGenerator _generator;
    private readonly StringOperator _defaultStringOperator;

    public FilterMethodBuilder(TypeBuilder typeBuilder, ILGenerator generator, StringOperator defaultStringOperator)
    {
        _typeBuilder = typeBuilder;
        _generator = generator;
        _defaultStringOperator = defaultStringOperator;
    }

    internal Type Build(
        uint fieldIndex = 0,
        ISet<string>? configuredEntityProperties = null,
        IReadOnlyList<CompareData<TFilter>>? compareList = null,
        IReadOnlyList<ContainData<TFilter>>? containList = null,
        IReadOnlyList<InData<TFilter>>? inList = null,
        IReadOnlyList<CompareStringData<TFilter>>? compareStringList = null,
        IReadOnlyList<FilterRangeData<TFilter>>? filterRangeList = null,
        IReadOnlyList<EntityRangeData<TFilter>>? entityRangeList = null)
    {
        var (compare, compareString, contain, isIn) = ExtractFilterProperties(fieldIndex, configuredEntityProperties);
        if (compareList != null && compareList.Any())
        {
            compare.AddRange(compareList);
        }

        if (compareStringList != null && compareStringList.Any())
        {
            compareString.AddRange(compareStringList);
        }

        if (containList != null && containList.Any())
        {
            contain.AddRange(containList);
        }

        if (inList != null && inList.Any())
        {
            isIn.AddRange(inList);
        }

        var filterConditionCount = GetFilteringConditionCount(compare, compareString, contain, isIn, filterRangeList, entityRangeList);
        if (filterConditionCount > 0)
        {
            // generate starting code
            var expressionLocal = _generator.DeclareLocal(typeof(Expression));
            _ = _generator.DeclareLocal(ParameterExpressionType);
            _generator.Emit(OpCodes.Ldnull);
            _generator.Emit(OpCodes.Stloc_0);
            _generator.Emit(OpCodes.Ldtoken, EntityType);
            _generator.Emit(OpCodes.Call, TypeOfMethod);
            _generator.Emit(OpCodes.Ldstr, "t");
            _generator.Emit(OpCodes.Call, ParameterMethod);
            _generator.Emit(OpCodes.Stloc_1);
            _generator.Emit(OpCodes.Ldloc_1);
            _generator.Emit(OpCodes.Newobj, ParameterReplacerConstructor);

            Dictionary<uint, CompareData<TFilter>>? compareData = default;
            if (compare.Any())
            {
                var buildCompareExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCompareExpression), Utilities.PublicStatic);
                var compareDictionaryField = _typeBuilder.DefineField(CompareDictionaryFieldName, typeof(Dictionary<uint, CompareData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in compare)
                {
                    void CallBuildExpressionMethod() => _generator.Emit(OpCodes.Call, buildCompareExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                    GenerateFieldFilterCode(compareDictionaryField, c.id, CompareFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                compareData = compare.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, CompareStringData<TFilter>>? compareStringData = default;
            if (compareString.Any())
            {
                var buildCompareStringExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildStringCompareExpression), Utilities.PublicStatic);
                var compareStringDictionaryField = _typeBuilder.DefineField(CompareStringDictionaryFieldName, typeof(Dictionary<uint, CompareStringData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in compareString)
                {
                    void CallBuildExpressionMethod() => _generator.Emit(OpCodes.Call, buildCompareStringExpressionMethod.MakeGenericMethod(FilterType));
                    GenerateFieldFilterCode(compareStringDictionaryField, c.id, CompareStringFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                compareStringData = compareString.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, ContainData<TFilter>>? containData = default;
            if (contain.Any())
            {
                var buildCollectionContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCollectionContainsExpression), Utilities.PublicStatic);
                var buildArrayContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildArrayContainsExpression), Utilities.PublicStatic);
                var containDictionaryField = _typeBuilder.DefineField(ContainDictionaryFieldName, typeof(Dictionary<uint, ContainData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var c in contain)
                {
                    void CallBuildExpressionMethod()
                    {
                        if (c.isCollection)
                        {
                            _generator.Emit(OpCodes.Call, buildCollectionContainExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                        }
                        else
                        {
                            _generator.Emit(OpCodes.Call, buildArrayContainExpressionMethod.MakeGenericMethod(FilterType, c.filterPropertyType));
                        }
                    }

                    GenerateFieldFilterCode(containDictionaryField, c.id, ContainFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                containData = contain.ToDictionary(c => c.id, c => c);
            }

            Dictionary<uint, InData<TFilter>>? inData = default;
            if (isIn.Any())
            {
                var buildInCollectionExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInCollectionExpression), Utilities.PublicStatic);
                var buildInArrayExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInArrayExpression), Utilities.PublicStatic);
                var inDictionaryField = _typeBuilder.DefineField(InDictionaryFieldName, typeof(Dictionary<uint, InData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var i in isIn)
                {
                    void CallBuildExpressionMethod()
                    {
                        if (i.isCollection)
                        {
                            _generator.Emit(OpCodes.Call, buildInCollectionExpressionMethod.MakeGenericMethod(FilterType, i.filterPropertyType));
                        }
                        else
                        {
                            _generator.Emit(OpCodes.Call, buildInArrayExpressionMethod.MakeGenericMethod(FilterType, i.filterPropertyType));
                        }
                    }

                    GenerateFieldFilterCode(inDictionaryField, i.id, InFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                inData = isIn.ToDictionary(i => i.id, i => i);
            }

            Dictionary<uint, FilterRangeData<TFilter>>? filterRangeData = default;
            if (filterRangeList != null && filterRangeList.Any())
            {
                var buildFilterRangeExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildFilterRangeExpression), Utilities.PublicStatic);
                var filterRangeDictionaryField = _typeBuilder.DefineField(FilterRangeDictionaryFieldName, typeof(Dictionary<uint, FilterRangeData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var f in filterRangeList)
                {
                    void CallBuildExpressionMethod() => _generator.Emit(OpCodes.Call, buildFilterRangeExpressionMethod.MakeGenericMethod(FilterType, f.filterMinPropertyType, f.filterMaxPropertyType));
                    GenerateFieldFilterCode(filterRangeDictionaryField, f.id, FilterRangeFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                filterRangeData = filterRangeList.ToDictionary(f => f.id, f => f);
            }

            Dictionary<uint, EntityRangeData<TFilter>>? entityRangeData = default;
            if (entityRangeList != null && entityRangeList.Any())
            {
                var buildEntityRangeExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildEntityRangeExpression), Utilities.PublicStatic);
                var entityRangeDictionaryField = _typeBuilder.DefineField(EntityRangeDictionaryFieldName, typeof(Dictionary<uint, EntityRangeData<TFilter>>), FieldAttributes.Private | FieldAttributes.Static);
                foreach (var e in entityRangeList)
                {
                    void CallBuildExpressionMethod() => _generator.Emit(OpCodes.Call, buildEntityRangeExpressionMethod.MakeGenericMethod(FilterType, e.filterPropertyType));
                    GenerateFieldFilterCode(entityRangeDictionaryField, e.id, EntityRangeFieldGetItem, expressionLocal, CallBuildExpressionMethod, ref filterConditionCount);
                }

                entityRangeData = entityRangeList.ToDictionary(e => e.id, e => e);
            }

            // Generate ending code
            _generator.Emit(OpCodes.Ldloc_0);
            _generator.Emit(OpCodes.Dup);
            var parameterJump = _generator.DefineLabel();
            _generator.Emit(OpCodes.Brtrue_S, parameterJump);
            _generator.Emit(OpCodes.Pop);
            _generator.Emit(OpCodes.Ldc_I4_1);
            _generator.Emit(OpCodes.Box, BooleanType);
            _generator.Emit(OpCodes.Call, ConstantExpressionMethod);
            _generator.MarkLabel(parameterJump);
            _generator.Emit(OpCodes.Ldc_I4_1);
            _generator.Emit(OpCodes.Newarr, ParameterExpressionType);
            _generator.Emit(OpCodes.Dup);
            _generator.Emit(OpCodes.Ldc_I4_0);
            _generator.Emit(OpCodes.Ldloc_1);
            _generator.Emit(OpCodes.Stelem_Ref);
            _generator.Emit(OpCodes.Call, LambdaMethod.MakeGenericMethod(typeof(Func<,>).MakeGenericType(EntityType, BooleanType)));
            _generator.Emit(OpCodes.Ret);

            var type = _typeBuilder.CreateType()!;

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

            if (filterRangeData != null)
            {
                WrapUpFilterCode(type, FilterRangeDictionaryFieldName, filterRangeData);
            }

            if (entityRangeData != null)
            {
                WrapUpFilterCode(type, EntityRangeDictionaryFieldName, entityRangeData);
            }

            return type;
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_1);
            _generator.Emit(OpCodes.Box, BooleanType);
            _generator.Emit(OpCodes.Call, ConstantExpressionMethod);
            _generator.Emit(OpCodes.Ldc_I4_1);
            _generator.Emit(OpCodes.Newarr, ParameterExpressionType);
            _generator.Emit(OpCodes.Dup);
            _generator.Emit(OpCodes.Ldc_I4_0);
            _generator.Emit(OpCodes.Ldtoken, EntityType);
            _generator.Emit(OpCodes.Call, TypeOfMethod);
            _generator.Emit(OpCodes.Ldstr, "t");
            _generator.Emit(OpCodes.Call, ParameterMethod);
            _generator.Emit(OpCodes.Stelem_Ref);
            _generator.Emit(OpCodes.Call, LambdaMethod.MakeGenericMethod(typeof(Func<,>).MakeGenericType(EntityType, BooleanType)));
            _generator.Emit(OpCodes.Ret);
            return _typeBuilder.CreateType()!;
        }
    }

    private static int GetFilteringConditionCount(
        List<CompareData<TFilter>> compareList,
        List<CompareStringData<TFilter>> compareStringList,
        List<ContainData<TFilter>> containList,
        List<InData<TFilter>> inList,
        IReadOnlyList<FilterRangeData<TFilter>>? filterRangeList,
        IReadOnlyList<EntityRangeData<TFilter>>? entityRangeList)
        => compareList.Count + compareStringList.Count + containList.Count + inList.Count + (filterRangeList?.Count ?? 0) + (entityRangeList?.Count ?? 0);

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
                    compareStringList.Add(new CompareStringData<TFilter>(fieldIndex++, entityPropertyExpression, _defaultStringOperator, filterPropertyFunc, null, null, ignoreIf));
                    continue;
                }

                var comparison = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, Operator.Equality);
                if (comparison != null)
                {
                    var c = comparison.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? BuildFilterPropertyIsDefaultFunction(filterProperty) : null;
                    compareList.Add(new CompareData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, c.leftConvertTo, Operator.Equality, filterPropertyFunc, filterPropertyType, c.rightConvertTo, null, null, ignoreIf));
                    continue;
                }

                var contains = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (contains != null)
                {
                    var value = contains.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? BuildFilterPropertyIsDefaultFunction(filterProperty) : null;
                    containList.Add(new ContainData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, value.containerItemType, Operator.Contains, filterPropertyFunc, filterPropertyType, value.itemConvertTo, value.isCollection, value.nullValueNotCovered, null, null, ignoreIf));
                    continue;
                }

                var isIn = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (isIn != null)
                {
                    var value = isIn.Value;
                    inList.Add(new InData<TFilter>(fieldIndex++, entityPropertyExpression, entityProperty.PropertyType, value.itemConvertTo, Operator.In, filterPropertyFunc, filterPropertyType, isIn.Value.containerItemType, value.isCollection, value.nullValueNotCovered, null, null, null));
                    continue;
                }
            }
        }

        return (compareList, compareStringList, containList, inList);
    }

    private void GenerateFieldFilterCode(
        FieldInfo dataDictionaryField,
        uint id,
        MethodInfo getItem,
        LocalBuilder expressionLocal,
        Action callBuildExpressionMethod,
        ref int filterConditionCount)
    {
        if (filterConditionCount > 1)
        {
            _generator.Emit(OpCodes.Dup);
        }

        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Ldsfld, dataDictionaryField);
        LoadUint(_generator, id);
        _generator.Emit(OpCodes.Callvirt, getItem);
        _generator.Emit(OpCodes.Ldloca_S, expressionLocal);
        callBuildExpressionMethod();
        filterConditionCount--;
    }
}
