namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class FilterTypeBuilder
{
    public const string FilterMethodName = "Filter";
    private readonly ModuleBuilder _moduleBuilder;

    public FilterTypeBuilder(ModuleBuilder moduleBuilder)
    {
        _moduleBuilder = moduleBuilder;
    }

    public FilterMethodBuilder<TEntity, TFilter> BuildFilterMethodBuilder<TEntity, TFilter>()
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
        return new FilterMethodBuilder<TEntity, TFilter>(typeBuilder, generator);
    }

    private static string GetDynamicTypeName(Type entityType, Type filterType) => $"Filter_${GetTypeName(entityType)}_${GetTypeName(filterType)}";

    private static string GetTypeName(Type type) => $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
}

internal record struct CompareData<TFilter>(
    PropertyInfo entityProperty,
    Type? entityPropertyConvertTo,
    FilterBy type,
    PropertyInfo filterProperty,
    Type? filterPropertyConvertTo,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct ContainData<TFilter>(
    PropertyInfo entityProperty,
    Type entityPropertyItemType,
    FilterBy type,
    PropertyInfo filterProperty,
    Type? filterPropertyConvertTo,
    bool isCollection,
    bool nullValueNotCovered,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct InData<TFilter>(
    PropertyInfo entityProperty,
    Type? entityPropertyConvertTo,
    FilterBy type,
    PropertyInfo filterProperty,
    Type filterPropertyItemType,
    bool isCollection,
    bool nullValueNotCovered,
    Func<TFilter, bool>? includeNull,
    Func<TFilter, bool>? reverseIf,
    Func<TFilter, bool>? ignoreIf)
    where TFilter : class;

internal record struct GeneratedFilterFields<TFilter, TDictionary>(
    TDictionary data,
    Dictionary<string, Func<TFilter, bool>> includeNull,
    Dictionary<string, Func<TFilter, bool>> ignoreIf,
    Dictionary<string, Func<TFilter, bool>> reverseIf)
    where TFilter : class;

internal sealed class FilterMethodBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private const string CompareDictionaryFieldName = "_compareDictionary";
    private const string ContainDictionaryFieldName = "_containDictionary";
    private const string InDictionaryFieldName = "_inDictionary";
    private const string FilterRangeDictionaryFieldName = "_filterRangeDictionary";
    private const string EntityRangeDictionaryFieldName = "_entityRangeDictionary";
    private const string DictionaryItemMethodName = "get_Item";
    private static readonly MethodInfo CompareFieldOuterGetItem = typeof(Dictionary<string, Dictionary<string, CompareData>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo CompareFieldInnerGetItem = typeof(Dictionary<Type, CompareData>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ContainFieldOuterGetItem = typeof(Dictionary<string, Dictionary<string, ContainData>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ContainFieldInnerGetItem = typeof(Dictionary<Type, ContainData>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo InFieldOuterGetItem = typeof(Dictionary<string, Dictionary<string, InData>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo InFieldInnerGetItem = typeof(Dictionary<Type, InData>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo RangeFieldLevel1GetItem = typeof(Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo RangeFieldLevel2GetItem = typeof(Dictionary<Type, Dictionary<string, RangeData>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo RangeFieldLevel3GetItem = typeof(Dictionary<Type, RangeData>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly Type EntityType = typeof(TEntity);
    private static readonly Type BooleanType = typeof(bool);
    private static readonly Type NullableBooleanType = typeof(bool?);
    private static readonly ConstructorInfo NullableBooleanConstructor = NullableBooleanType.GetConstructor(new[] { typeof(bool) });
    private static readonly Type ParameterExpressionType = typeof(ParameterExpression);
    private static readonly MethodInfo TypeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), Utilities.PublicStatic)!;
    private static readonly MethodInfo ParameterMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == nameof(Expression.Parameter) && m.GetParameters().Length == 2)!;
    private static readonly MethodInfo LambdaMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == "Lambda" && m.GetParameters().Length == 2);
    private static readonly MethodInfo ConstantExpressionMethod = typeof(Expression).GetMethods(Utilities.PublicStatic).First(m => m.Name == "Constant" && m.GetParameters().Length == 1);
    private static readonly MethodInfo FilterConditionInvoke = typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType).GetMethod("Invoke", Utilities.PublicInstance);
    private static readonly MethodInfo BuildCompareExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCompareExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildArrayContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildArrayContainsExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildCollectionContainExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildCollectionContainsExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildInArrayExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInArrayExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildInCollectionExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildInCollectionExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildFilterRangeExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildFilterRangeExpression), Utilities.PublicStatic);
    private static readonly MethodInfo BuildEntityRangeExpressionMethod = typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.BuildEntityRangeExpression), Utilities.PublicStatic);
    private readonly TypeBuilder _typeBuilder;
    private readonly ILGenerator _generator;

    public FilterMethodBuilder(TypeBuilder typeBuilder, ILGenerator generator)
    {
        _typeBuilder = typeBuilder;
        _generator = generator;
    }

    internal Type Build(
        ISet<string>? configuredEntityProperties,
        ISet<string>? configuredFilterProperties,
        IReadOnlyList<CompareData<TFilter>>? compareList,
        IReadOnlyList<ContainData<TFilter>>? containList,
        IReadOnlyList<InData<TFilter>>? inList,
        IReadOnlyList<FilterRangeData<TFilter>>? filterRangeList,
        IReadOnlyList<EntityRangeData<TFilter>>? entityRangeList)
    {
        var (compare, contain, isIn) = ExtractFilterProperties(configuredEntityProperties, configuredFilterProperties);
        if (compareList != null && compareList.Any())
        {
            compare.AddRange(compareList);
        }

        if (containList != null && containList.Any())
        {
            contain.AddRange(containList);
        }

        if (inList != null && inList.Any())
        {
            isIn.AddRange(inList);
        }

        var includeNullLocalVariableCount = GetIncludeNullLocalVariableCount(compare, contain, isIn, filterRangeList, entityRangeList);

        // generate starting code
        LocalBuilder? includeNullLocal1 = null;
        LocalBuilder? includeNullLocal2 = null;
        var expressionLocal = _generator.DeclareLocal(typeof(Expression));
        _ = _generator.DeclareLocal(ParameterExpressionType);
        if (includeNullLocalVariableCount >= 1)
        {
            includeNullLocal1 = _generator.DeclareLocal(NullableBooleanType);
        }

        if (includeNullLocalVariableCount == 2)
        {
            includeNullLocal2 = _generator.DeclareLocal(NullableBooleanType);
        }

        _generator.Emit(OpCodes.Ldnull);
        _generator.Emit(OpCodes.Stloc_0);
        _generator.Emit(OpCodes.Ldtoken, EntityType);
        _generator.Emit(OpCodes.Call, TypeOfMethod);
        _generator.Emit(OpCodes.Ldstr, "t");
        _generator.Emit(OpCodes.Call, ParameterMethod);
        _generator.Emit(OpCodes.Stloc_1);

        GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, CompareData>>>? compareFields = default;
        if (compare.Any())
        {
            compareFields = GenerateCompareCode(compare, includeNullLocal1!, expressionLocal);
        }

        GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, ContainData>>>? containFields = default;
        if (contain.Any())
        {
            containFields = GenerateContainCode(contain, includeNullLocal1!, expressionLocal);
        }

        GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, InData>>>? inFields = default;
        if (isIn.Any())
        {
            inFields = GenerateInCode(isIn, includeNullLocal1!, expressionLocal);
        }

        GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>>? filterRangeFields = default;
        if (filterRangeList != null && filterRangeList.Any())
        {
            filterRangeFields = GenerateFilterRangeCode(filterRangeList, includeNullLocal1!, expressionLocal);
        }

        GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>>? entityRangeFields = default;
        if (entityRangeList != null && entityRangeList.Any())
        {
            entityRangeFields = GenerateEntityRangeCode(entityRangeList, includeNullLocal1!, includeNullLocal2!, expressionLocal);
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

        if (compareFields.HasValue)
        {
            WrapUpFilterCode(type, CompareDictionaryFieldName, compareFields.Value);
        }

        if (containFields.HasValue)
        {
            WrapUpFilterCode(type, ContainDictionaryFieldName, containFields.Value);
        }

        if (inFields.HasValue)
        {
            WrapUpFilterCode(type, InDictionaryFieldName, inFields.Value);
        }

        if (filterRangeFields.HasValue)
        {
            WrapUpFilterCode(type, FilterRangeDictionaryFieldName, filterRangeFields.Value);
        }

        if (entityRangeFields.HasValue)
        {
            WrapUpFilterCode(type, EntityRangeDictionaryFieldName, entityRangeFields.Value);
        }

        return type;
    }

    private static int GetIncludeNullLocalVariableCount(
        List<CompareData<TFilter>> compareList,
        List<ContainData<TFilter>> containList,
        List<InData<TFilter>> inList,
        IReadOnlyList<FilterRangeData<TFilter>>? filterRangeList,
        IReadOnlyList<EntityRangeData<TFilter>>? entityRangeList)
        => entityRangeList != null && entityRangeList.Any()
            ? 2
            : compareList.Any() || containList.Any() || inList.Any() || (filterRangeList != null && filterRangeList.Any())
                ? 1
                : 0;

    private static (List<CompareData<TFilter>>, List<ContainData<TFilter>>, List<InData<TFilter>>) ExtractFilterProperties(ISet<string>? configuredEntityProperties, ISet<string>? configuredFilterProperties)
    {
        var filterProperties = typeof(TFilter).GetProperties(Utilities.PublicInstance)
            .Where(p => p.GetMethod != default && (configuredFilterProperties == null || !configuredFilterProperties.Contains(p.Name)));
        var entityProperties = typeof(TEntity).GetProperties(Utilities.PublicInstance)
            .Where(p => p.GetMethod != default && (configuredEntityProperties == null || !configuredEntityProperties.Contains(p.Name)))
            .ToDictionary(p => p.Name, p => p);

        var compareList = new List<CompareData<TFilter>>();
        var containList = new List<ContainData<TFilter>>();
        var inList = new List<InData<TFilter>>();
        foreach (var filterProperty in filterProperties)
        {
            var filterPropertyType = filterProperty.PropertyType;
            if (entityProperties.TryGetValue(filterProperty.Name, out var entityProperty))
            {
                var entityPropertyType = entityProperty.PropertyType;
                var comparison = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, FilterBy.Equality);
                if (comparison != null)
                {
                    var c = comparison.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    compareList.Add(new CompareData<TFilter>(entityProperty, c.leftConvertTo, FilterBy.Equality, filterProperty, c.rightConvertTo, null, null, ignoreIf));
                    continue;
                }

                var contains = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (contains != null)
                {
                    var value = contains.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _)
                        ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    containList.Add(new ContainData<TFilter>(entityProperty, value.containerItemType, FilterBy.Contains, filterProperty, value.itemConvertTo, value.isCollection, value.nullValueNotCovered, null, null, ignoreIf));
                    continue;
                }

                var isIn = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (isIn != null)
                {
                    var value = isIn.Value;
                    inList.Add(new InData<TFilter>(entityProperty, value.itemConvertTo, FilterBy.In, filterProperty, isIn.Value.containerItemType, value.isCollection, value.nullValueNotCovered, null, null, null));
                    continue;
                }
            }
        }

        return (compareList, containList, inList);
    }

    private static void WrapUpFilterCode<TDictionary>(
        Type type,
        string fieldName,
        GeneratedFilterFields<TFilter, TDictionary> fields)
    {
        type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, fields.data);
        foreach (var includeNull in fields.includeNull)
        {
            type.GetField(includeNull.Key, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, includeNull.Value);
        }

        foreach (var ignore in fields.ignoreIf)
        {
            type.GetField(ignore.Key, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, ignore.Value);
        }

        foreach (var reverse in fields.reverseIf)
        {
            type.GetField(reverse.Key, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, reverse.Value);
        }
    }

    private static FilterBy Opposite(FilterByRange type)
    {
        return type switch
        {
            FilterByRange.LessThan => FilterBy.GreaterThan,
            _ => FilterBy.GreaterThanOrEqual,
        };
    }

    private static FilterBy Convert(FilterByRange type)
    {
        return type switch
        {
            FilterByRange.LessThan => FilterBy.LessThan,
            _ => FilterBy.LessThanOrEqual,
        };
    }

    private GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, CompareData>>> GenerateCompareCode(IList<CompareData<TFilter>> compare, LocalBuilder includeNullLocal, LocalBuilder expressionLocal)
    {
        var compareDictionaryField = _typeBuilder.DefineField(CompareDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, CompareData>>), FieldAttributes.Private | FieldAttributes.Static);
        var compareDictionary = new Dictionary<string, Dictionary<string, CompareData>>();
        var includeNullFields = new Dictionary<string, Func<TFilter, bool>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var c in compare)
        {
            var fields = PrepareMethodFields(c.entityProperty.Name, c.filterProperty.Name, c.includeNull, c.ignoreIf, c.reverseIf, includeNullFields, ignoreIfFields, reverseIfFields);
            compareDictionary.Add(c.entityProperty.Name, c.filterProperty.Name, new (c.entityPropertyConvertTo, c.filterPropertyConvertTo, c.type));
            void CallBuildExpressionMethod() => _generator.Emit(OpCodes.Call, BuildCompareExpressionMethod.MakeGenericMethod(c.entityProperty.PropertyType, c.filterProperty.PropertyType));
            GenerateFieldFilterCode(
                compareDictionaryField,
                fields.Item1,
                fields.Item2,
                fields.Item3,
                c.entityProperty,
                c.filterProperty,
                includeNullLocal,
                expressionLocal,
                CompareFieldOuterGetItem,
                CompareFieldInnerGetItem,
                CallBuildExpressionMethod);
        }

        return new (compareDictionary, includeNullFields, ignoreIfFields, reverseIfFields);
    }

    private GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, ContainData>>> GenerateContainCode(IList<ContainData<TFilter>> contain, LocalBuilder includeNullLocal, LocalBuilder expressionLocal)
    {
        var containDictionaryField = _typeBuilder.DefineField(ContainDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, ContainData>>), FieldAttributes.Private | FieldAttributes.Static);
        var containDictionary = new Dictionary<string, Dictionary<string, ContainData>>();
        var includeNullFields = new Dictionary<string, Func<TFilter, bool>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var c in contain)
        {
            var fields = PrepareMethodFields(c.entityProperty.Name, c.filterProperty.Name, c.includeNull, c.ignoreIf, c.reverseIf, includeNullFields, ignoreIfFields, reverseIfFields);
            containDictionary.Add(c.entityProperty.Name, c.filterProperty.Name, new (c.filterPropertyConvertTo, c.type, c.nullValueNotCovered));
            void CallBuildExpressionMethod()
            {
                if (c.isCollection)
                {
                    _generator.Emit(OpCodes.Call, BuildCollectionContainExpressionMethod.MakeGenericMethod(c.entityPropertyItemType, c.filterProperty.PropertyType));
                }
                else
                {
                    _generator.Emit(OpCodes.Call, BuildArrayContainExpressionMethod.MakeGenericMethod(c.entityPropertyItemType, c.filterProperty.PropertyType));
                }
            }

            GenerateFieldFilterCode(
                containDictionaryField,
                fields.Item1,
                fields.Item2,
                fields.Item3,
                c.entityProperty,
                c.filterProperty,
                includeNullLocal,
                expressionLocal,
                ContainFieldOuterGetItem,
                ContainFieldInnerGetItem,
                CallBuildExpressionMethod);
        }

        return new (containDictionary, includeNullFields, ignoreIfFields, reverseIfFields);
    }

    private GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, InData>>> GenerateInCode(IList<InData<TFilter>> isIn, LocalBuilder includeNullLocal, LocalBuilder expressionLocal)
    {
        var inDictionaryField = _typeBuilder.DefineField(InDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, InData>>), FieldAttributes.Private | FieldAttributes.Static);
        var inDictionary = new Dictionary<string, Dictionary<string, InData>>();
        var includeNullFields = new Dictionary<string, Func<TFilter, bool>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var i in isIn)
        {
            var fields = PrepareMethodFields(i.entityProperty.Name, i.filterProperty.Name, i.includeNull, i.ignoreIf, i.reverseIf, includeNullFields, ignoreIfFields, reverseIfFields);
            inDictionary.Add(i.entityProperty.Name, i.filterProperty.Name, new (i.entityPropertyConvertTo, i.type, i.nullValueNotCovered));
            void CallBuildExpressionMethod()
            {
                if (i.isCollection)
                {
                    _generator.Emit(OpCodes.Call, BuildInCollectionExpressionMethod.MakeGenericMethod(i.entityProperty.PropertyType, i.filterPropertyItemType));
                }
                else
                {
                    _generator.Emit(OpCodes.Call, BuildInArrayExpressionMethod.MakeGenericMethod(i.entityProperty.PropertyType, i.filterPropertyItemType));
                }
            }

            GenerateFieldFilterCode(
                inDictionaryField,
                fields.Item1,
                fields.Item2,
                fields.Item3,
                i.entityProperty,
                i.filterProperty,
                includeNullLocal,
                expressionLocal,
                InFieldOuterGetItem,
                InFieldInnerGetItem,
                CallBuildExpressionMethod);
        }

        return new (inDictionary, includeNullFields, ignoreIfFields, reverseIfFields);
    }

    private GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>> GenerateFilterRangeCode(IReadOnlyList<FilterRangeData<TFilter>> range, LocalBuilder includeNullLocal, LocalBuilder expressionLocal)
    {
        var filterRangeDictionaryField = _typeBuilder.DefineField(FilterRangeDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>), FieldAttributes.Private | FieldAttributes.Static);
        var filterRangeDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>();
        var includeNullFields = new Dictionary<string, Func<TFilter, bool>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();

        foreach (var f in range)
        {
            FieldInfo? includeNullField = null;
            FieldInfo? ignoreMinIfField = null;
            FieldInfo? ignoreMaxIfField = null;
            FieldInfo? reverseIfField = null;
            var filterName = $"{f.entityProperty.Name}_{f.filterMinProperty.Name}_{f.filterMaxProperty.Name}";

            if (f.includeNull != null)
            {
                var fieldName = $"_includeNull_{filterName}";
                includeNullField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                includeNullFields.Add(fieldName, f.includeNull);
            }

            if (f.ignoreMinIf != null)
            {
                var fieldName = $"_ignore_{filterName}_Min";
                ignoreMinIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, f.ignoreMinIf);
            }

            if (f.ignoreMaxIf != null)
            {
                var fieldName = $"_ignore_{filterName}_Max";
                ignoreMaxIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, f.ignoreMaxIf);
            }

            if (f.reverseIf != null)
            {
                var fieldName = $"_reverse_{filterName}_If";
                reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                reverseIfFields.Add(fieldName, f.reverseIf);
            }

            filterRangeDictionary.Add(
                f.entityProperty.Name,
                f.filterMinProperty.Name,
                f.filterMaxProperty.Name,
                new (new (f.entityMinPropertyConvertTo, f.filterMinPropertyConvertTo, Opposite(f.filterMinType)), new (f.entityMaxPropertyConvertTo, f.filterMaxPropertyConvertTo, Convert(f.filterMaxType))));

            GenerateFilterRangeFilterCode(
                filterRangeDictionaryField,
                includeNullField,
                ignoreMinIfField,
                ignoreMaxIfField,
                reverseIfField,
                f.entityProperty,
                f.filterMinProperty,
                f.filterMaxProperty,
                includeNullLocal,
                expressionLocal);
        }

        return new (filterRangeDictionary, includeNullFields, ignoreIfFields, reverseIfFields);
    }

    private GeneratedFilterFields<TFilter, Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>> GenerateEntityRangeCode(IReadOnlyList<EntityRangeData<TFilter>> range, LocalBuilder includeNullLocal1, LocalBuilder includeNullLocal2, LocalBuilder expressionLocal)
    {
        var entityRangeDictionaryField = _typeBuilder.DefineField(EntityRangeDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>), FieldAttributes.Private | FieldAttributes.Static);
        var entityRangeDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, RangeData>>>();
        var includeNullFields = new Dictionary<string, Func<TFilter, bool>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();

        foreach (var e in range)
        {
            FieldInfo? includeNullMinField = null;
            FieldInfo? includeNullMaxField = null;
            FieldInfo? ignoreIfField = null;
            FieldInfo? reverseIfField = null;
            var filterName = $"{e.entityMinProperty.Name}_{e.entityMaxProperty.Name}_{e.filterProperty.Name}";

            if (e.includeNullMin != null)
            {
                var fieldName = $"_includeNull_{filterName}_Min";
                includeNullMinField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                includeNullFields.Add(fieldName, e.includeNullMin);
            }

            if (e.includeNullMax != null)
            {
                var fieldName = $"_includeNull_{filterName}_Max";
                includeNullMaxField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                includeNullFields.Add(fieldName, e.includeNullMax);
            }

            if (e.ignoreIf != null)
            {
                var fieldName = $"_ignore_{filterName}";
                ignoreIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, e.ignoreIf);
            }

            if (e.reverseIf != null)
            {
                var fieldName = $"_reverse_{filterName}_If";
                reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
                reverseIfFields.Add(fieldName, e.reverseIf);
            }

            entityRangeDictionary.Add(
                e.entityMinProperty.Name,
                e.entityMaxProperty.Name,
                e.filterProperty.Name,
                new (new (e.entityMinPropertyConvertTo, e.filterMinPropertyConvertTo, Convert(e.entityMinType)), new (e.entityMaxPropertyConvertTo, e.filterMaxPropertyConvertTo, Opposite(e.entityMaxType))));

            GenerateEntityRangeFilterCode(
                entityRangeDictionaryField,
                includeNullMinField,
                includeNullMaxField,
                ignoreIfField,
                reverseIfField,
                e.entityMinProperty,
                e.entityMaxProperty,
                e.filterProperty,
                includeNullLocal1,
                includeNullLocal2,
                expressionLocal);
        }

        return new (entityRangeDictionary, includeNullFields, ignoreIfFields, reverseIfFields);
    }

    private (FieldInfo?, FieldInfo?, FieldInfo?) PrepareMethodFields(
        string entityPropertyName,
        string filterPropertyName,
        Func<TFilter, bool>? includeNull,
        Func<TFilter, bool>? ignoreIf,
        Func<TFilter, bool>? reverseIf,
        Dictionary<string, Func<TFilter, bool>> includeNullFields,
        Dictionary<string, Func<TFilter, bool>> ignoreIfFields,
        Dictionary<string, Func<TFilter, bool>> reverseIfFields)
    {
        FieldInfo? includeNullField = null;
        FieldInfo? ignoreIfField = null;
        FieldInfo? reverseIfField = null;
        var filterName = $"{entityPropertyName}_{filterPropertyName}";

        if (includeNull != null)
        {
            var fieldName = $"_includeNull_{filterName}";
            includeNullField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
            includeNullFields.Add(fieldName, includeNull);
        }

        if (ignoreIf != null)
        {
            var fieldName = $"_ignore_{filterName}_If";
            ignoreIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
            ignoreIfFields.Add(fieldName, ignoreIf);
        }

        if (reverseIf != null)
        {
            var fieldName = $"_reverse_{filterName}_If";
            reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), BooleanType), FieldAttributes.Private | FieldAttributes.Static);
            reverseIfFields.Add(fieldName, reverseIf);
        }

        return (includeNullField, ignoreIfField, reverseIfField);
    }

    private void GenerateFieldFilterCode(
        FieldInfo dataDictionaryField,
        FieldInfo? includeNullField,
        FieldInfo? ignoreIfField,
        FieldInfo? reverseIfField,
        PropertyInfo entityProperty,
        PropertyInfo filterProperty,
        LocalBuilder includeNullLocal,
        LocalBuilder expressionLocal,
        MethodInfo outterGetItem,
        MethodInfo innerGetItem,
        Action callBuildExpressionMethod)
    {
        Label endIfIgnore = default;
        if (ignoreIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, ignoreIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            endIfIgnore = _generator.DefineLabel();
            _generator.Emit(OpCodes.Brtrue_S, endIfIgnore);
        }

        _generator.Emit(OpCodes.Ldloc_1);
        _generator.Emit(OpCodes.Ldstr, entityProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, filterProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, dataDictionaryField);
        _generator.Emit(OpCodes.Ldstr, entityProperty.Name);
        _generator.Emit(OpCodes.Callvirt, outterGetItem);
        _generator.Emit(OpCodes.Ldstr, filterProperty.Name);
        _generator.Emit(OpCodes.Callvirt, innerGetItem);
        if (includeNullField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, includeNullField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            _generator.Emit(OpCodes.Newobj, NullableBooleanConstructor);
        }
        else
        {
            _generator.Emit(OpCodes.Ldloca_S, includeNullLocal);
            _generator.Emit(OpCodes.Initobj, NullableBooleanType);
            _generator.Emit(OpCodes.Ldloc_2);
        }

        if (reverseIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, reverseIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_0);
        }

        _generator.Emit(OpCodes.Ldloca_S, expressionLocal);
        callBuildExpressionMethod();

        if (ignoreIfField != null)
        {
            _generator.MarkLabel(endIfIgnore);
        }
    }

    private void GenerateFilterRangeFilterCode(
        FieldInfo filterRangeDictionary,
        FieldInfo? includeNullField,
        FieldInfo? ignoreMinIfField,
        FieldInfo? ignoreMaxIfField,
        FieldInfo? reverseIfField,
        PropertyInfo entityProperty,
        PropertyInfo filterMinProperty,
        PropertyInfo filterMaxProperty,
        LocalBuilder includeNullLocal,
        LocalBuilder expressionLocal)
    {
        _generator.Emit(OpCodes.Ldloc_1);
        _generator.Emit(OpCodes.Ldstr, entityProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, filterMinProperty.GetMethod);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, filterMaxProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, filterRangeDictionary);
        _generator.Emit(OpCodes.Ldstr, entityProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel1GetItem);
        _generator.Emit(OpCodes.Ldstr, filterMinProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel2GetItem);
        _generator.Emit(OpCodes.Ldstr, filterMaxProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel3GetItem);
        if (ignoreMinIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, ignoreMinIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_0);
        }

        if (ignoreMaxIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, ignoreMaxIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_0);
        }

        if (includeNullField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, includeNullField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            _generator.Emit(OpCodes.Newobj, NullableBooleanConstructor);
        }
        else
        {
            _generator.Emit(OpCodes.Ldloca_S, includeNullLocal);
            _generator.Emit(OpCodes.Initobj, NullableBooleanType);
            _generator.Emit(OpCodes.Ldloc_2);
        }

        if (reverseIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, reverseIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_0);
        }

        _generator.Emit(OpCodes.Ldloca_S, expressionLocal);
        _generator.Emit(OpCodes.Call, BuildFilterRangeExpressionMethod.MakeGenericMethod(entityProperty.PropertyType, filterMinProperty.PropertyType, filterMaxProperty.PropertyType));
    }

    private void GenerateEntityRangeFilterCode(
        FieldInfo entityRangeDictionary,
        FieldInfo? includeNullMinField,
        FieldInfo? includeNullMaxField,
        FieldInfo? ignoreIfField,
        FieldInfo? reverseIfField,
        PropertyInfo entityMinProperty,
        PropertyInfo entityMaxProperty,
        PropertyInfo filterProperty,
        LocalBuilder includeNullLocal1,
        LocalBuilder includeNullLocal2,
        LocalBuilder expressionLocal)
    {
        Label endIfIgnore = default;
        if (ignoreIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, ignoreIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            endIfIgnore = _generator.DefineLabel();
            _generator.Emit(OpCodes.Brtrue_S, endIfIgnore);
        }

        _generator.Emit(OpCodes.Ldloc_1);
        _generator.Emit(OpCodes.Ldstr, entityMinProperty.Name);
        _generator.Emit(OpCodes.Ldstr, entityMaxProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, filterProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, entityRangeDictionary);
        _generator.Emit(OpCodes.Ldstr, entityMinProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel1GetItem);
        _generator.Emit(OpCodes.Ldstr, entityMaxProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel2GetItem);
        _generator.Emit(OpCodes.Ldstr, filterProperty.Name);
        _generator.Emit(OpCodes.Callvirt, RangeFieldLevel3GetItem);
        if (includeNullMinField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, includeNullMinField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            _generator.Emit(OpCodes.Newobj, NullableBooleanConstructor);
        }
        else
        {
            _generator.Emit(OpCodes.Ldloca_S, includeNullLocal1);
            _generator.Emit(OpCodes.Initobj, NullableBooleanType);
            _generator.Emit(OpCodes.Ldloc_2);
        }

        if (includeNullMaxField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, includeNullMaxField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
            _generator.Emit(OpCodes.Newobj, NullableBooleanConstructor);
        }
        else
        {
            _generator.Emit(OpCodes.Ldloca_S, includeNullLocal2);
            _generator.Emit(OpCodes.Initobj, NullableBooleanType);
            _generator.Emit(OpCodes.Ldloc_3);
        }

        if (reverseIfField != null)
        {
            _generator.Emit(OpCodes.Ldsfld, reverseIfField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Callvirt, FilterConditionInvoke);
        }
        else
        {
            _generator.Emit(OpCodes.Ldc_I4_0);
        }

        _generator.Emit(OpCodes.Ldloca_S, expressionLocal);
        _generator.Emit(OpCodes.Call, BuildEntityRangeExpressionMethod.MakeGenericMethod(entityMinProperty.PropertyType, entityMaxProperty.PropertyType, filterProperty.PropertyType));

        if (ignoreIfField != null)
        {
            _generator.MarkLabel(endIfIgnore);
        }
    }
}
