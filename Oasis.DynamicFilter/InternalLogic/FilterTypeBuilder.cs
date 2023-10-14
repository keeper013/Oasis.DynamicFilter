﻿namespace Oasis.DynamicFilter.InternalLogic;

using System;
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

internal sealed class FilterMethodBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private const string CompareDictionaryFieldName = "_compareDictionary";
    private const string ContainDictionaryFieldName = "_containDictionary";
    private const string InDictionaryFieldName = "_inDictionary";
    private const string DictionaryItemMethodName = "get_Item";
    private static readonly MethodInfo CompareFieldOuterGetItem = typeof(Dictionary<string, Dictionary<string, (Type?, Type?, FilterByPropertyType)>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo CompareFieldInnerGetItem = typeof(Dictionary<Type, (Type?, Type?, FilterByPropertyType)>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly Type EntityType = typeof(TEntity);
    private static readonly Type BooleanType = typeof(bool);
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
        // TODO: code refine
        var compareFields = GenerateCompareCode(compare, expressionLocal);
        var containFields = GenerateContainCode(contain, expressionLocal);
        var inFields = GenerateInCode(isIn, expressionLocal);

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

        WrapUpFilterCode(type, CompareDictionaryFieldName, compareFields.Item1, compareFields.Item2, compareFields.Item3);
        WrapUpFilterCode(type, ContainDictionaryFieldName, containFields.Item1, containFields.Item2, containFields.Item3);
        WrapUpFilterCode(type, InDictionaryFieldName, inFields.Item1, inFields.Item2, inFields.Item3);
        return type;
    }

    private static (List<CompareData<TFilter>>, List<ContainData<TFilter>>, List<InData<TFilter>>) ExtractFilterProperties(ISet<string>? configuredEntityProperties, ISet<string>? configuredFilterProperties)
    {
        var filterProperties = typeof(TFilter).GetProperties(Utilities.PublicInstance)
            .Where(p => p.PropertyType.IsFilterableType() && p.GetMethod != default && (configuredFilterProperties == null || !configuredFilterProperties.Contains(p.Name)));
        var entityProperties = typeof(TEntity).GetProperties(Utilities.PublicInstance)
            .Where(p => p.PropertyType.IsFilterableType() && p.GetMethod != default && (configuredEntityProperties == null || !configuredEntityProperties.Contains(p.Name)))
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
                var comparison = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, FilterByPropertyType.Equality);
                if (comparison != null)
                {
                    var c = comparison.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    compareList.Add(new CompareData<TFilter>(entityProperty, c.Item1, FilterByPropertyType.Equality, filterProperty, c.Item2, null, ignoreIf));
                    continue;
                }

                var contains = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (contains != null)
                {
                    var value = contains.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    containList.Add(new ContainData<TFilter>(entityProperty, value.Item1, FilterByPropertyType.Contains, filterProperty, value.Item2, value.Item3, null, ignoreIf));
                    continue;
                }

                var isIn = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (isIn != null)
                {
                    var value = isIn.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    inList.Add(new InData<TFilter>(entityProperty, value.Item2, FilterByPropertyType.In, filterProperty, isIn.Value.Item1, value.Item3, null, ignoreIf));
                    continue;
                }
            }
        }

        return (compareList, containList, inList);
    }

    private static void WrapUpFilterCode<TDictionary>(
        Type type,
        string fieldName,
        TDictionary dictionary,
        Dictionary<string, Func<TFilter, bool>> ignoreIfFields,
        Dictionary<string, Func<TFilter, bool>> reverseIfFields)
    {
        type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, dictionary);
        foreach (var ignore in ignoreIfFields)
        {
            type.GetField(ignore.Key, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, ignore.Value);
        }

        foreach (var reverse in reverseIfFields)
        {
            type.GetField(reverse.Key, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, reverse.Value);
        }
    }

    private (Dictionary<string, Dictionary<string, (Type?, Type?, FilterByPropertyType)>>, Dictionary<string, Func<TFilter, bool>>, Dictionary<string, Func<TFilter, bool>>) GenerateCompareCode(IList<CompareData<TFilter>> compare, LocalBuilder expressionLocal)
    {
        var compareDictionaryField = _typeBuilder.DefineField(CompareDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, (Type?, Type?, FilterByPropertyType)>>), FieldAttributes.Private | FieldAttributes.Static);
        var compareDictionary = new Dictionary<string, Dictionary<string, (Type?, Type?, FilterByPropertyType)>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var c in compare)
        {
            FieldInfo? ignoreIfField = null;
            FieldInfo? reverseIfField = null;
            if (c.ignoreIf != null)
            {
                var fieldName = $"_ignore_{c.entityProperty.Name}_{c.filterProperty.Name}_If";
                ignoreIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, c.ignoreIf);
            }

            if (c.reverseIf != null)
            {
                var fieldName = $"_reverse_{c.entityProperty.Name}_{c.filterProperty.Name}_If";
                reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                reverseIfFields.Add(fieldName, c.reverseIf);
            }

            compareDictionary.Add(c.entityProperty.Name, c.filterProperty.Name, (c.entityPropertyConvertTo, c.filterPropertyConvertTo, c.type));
            GenerateFieldCompareCode(c, compareDictionaryField, ignoreIfField, reverseIfField, expressionLocal);
        }

        return (compareDictionary, ignoreIfFields, reverseIfFields);
    }

    private void GenerateFieldCompareCode(CompareData<TFilter> compareData, FieldInfo compareDictionaryField, FieldInfo? ignoreIfField, FieldInfo? reverseIfField, LocalBuilder expressionLocal)
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
        _generator.Emit(OpCodes.Ldstr, compareData.entityProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, compareData.filterProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, compareDictionaryField);
        _generator.Emit(OpCodes.Ldstr, compareData.entityProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldOuterGetItem);
        _generator.Emit(OpCodes.Ldstr, compareData.filterProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldInnerGetItem);
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
        _generator.Emit(OpCodes.Call, BuildCompareExpressionMethod.MakeGenericMethod(compareData.filterProperty.PropertyType));

        if (ignoreIfField != null)
        {
            _generator.MarkLabel(endIfIgnore);
        }
    }

    private (Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>, Dictionary<string, Func<TFilter, bool>>, Dictionary<string, Func<TFilter, bool>>) GenerateContainCode(IList<ContainData<TFilter>> contain, LocalBuilder expressionLocal)
    {
        var containDictionaryField = _typeBuilder.DefineField(ContainDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>), FieldAttributes.Private | FieldAttributes.Static);
        var containDictionary = new Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var c in contain)
        {
            FieldInfo? ignoreIfField = null;
            FieldInfo? reverseIfField = null;
            if (c.ignoreIf != null)
            {
                var fieldName = $"_ignore_{c.entityProperty.Name}_{c.filterProperty.Name}_If";
                ignoreIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, c.ignoreIf);
            }

            if (c.reverseIf != null)
            {
                var fieldName = $"_reverse_{c.entityProperty.Name}_{c.filterProperty.Name}_If";
                reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                reverseIfFields.Add(fieldName, c.reverseIf);
            }

            containDictionary.Add(c.entityProperty.Name, c.filterProperty.Name, (c.filterPropertyConvertTo, c.type));
            GenerateFieldContainCode(c, containDictionaryField, ignoreIfField, reverseIfField, expressionLocal);
        }

        return (containDictionary, ignoreIfFields, reverseIfFields);
    }

    private void GenerateFieldContainCode(ContainData<TFilter> containData, FieldInfo containDictionaryField, FieldInfo? ignoreIfField, FieldInfo? reverseIfField, LocalBuilder expressionLocal)
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
        _generator.Emit(OpCodes.Ldstr, containData.entityProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, containData.filterProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, containDictionaryField);
        _generator.Emit(OpCodes.Ldstr, containData.entityProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldOuterGetItem);
        _generator.Emit(OpCodes.Ldstr, containData.filterProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldInnerGetItem);
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
        if (containData.isCollection)
        {
            _generator.Emit(OpCodes.Call, BuildCollectionContainExpressionMethod.MakeGenericMethod(containData.entityPropertyItemType, containData.filterProperty.PropertyType));
        }
        else
        {
            _generator.Emit(OpCodes.Call, BuildArrayContainExpressionMethod.MakeGenericMethod(containData.entityPropertyItemType, containData.filterProperty.PropertyType));
        }

        if (ignoreIfField != null)
        {
            _generator.MarkLabel(endIfIgnore);
        }
    }

    private (Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>, Dictionary<string, Func<TFilter, bool>>, Dictionary<string, Func<TFilter, bool>>) GenerateInCode(IList<InData<TFilter>> isIn, LocalBuilder expressionLocal)
    {
        var inDictionaryField = _typeBuilder.DefineField(InDictionaryFieldName, typeof(Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>), FieldAttributes.Private | FieldAttributes.Static);
        var inDictionary = new Dictionary<string, Dictionary<string, (Type?, FilterByPropertyType)>>();
        var ignoreIfFields = new Dictionary<string, Func<TFilter, bool>>();
        var reverseIfFields = new Dictionary<string, Func<TFilter, bool>>();
        foreach (var i in isIn)
        {
            FieldInfo? ignoreIfField = null;
            FieldInfo? reverseIfField = null;
            if (i.ignoreIf != null)
            {
                var fieldName = $"_ignore_{i.entityProperty.Name}_{i.filterProperty.Name}_If";
                ignoreIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                ignoreIfFields.Add(fieldName, i.ignoreIf);
            }

            if (i.reverseIf != null)
            {
                var fieldName = $"_reverse_{i.entityProperty.Name}_{i.filterProperty.Name}_If";
                reverseIfField = _typeBuilder.DefineField(fieldName, typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(bool)), FieldAttributes.Private | FieldAttributes.Static);
                reverseIfFields.Add(fieldName, i.reverseIf);
            }

            inDictionary.Add(i.entityProperty.Name, i.filterProperty.Name, (i.entityPropertyConvertTo, i.type));
            GenerateFieldInCode(i, inDictionaryField, ignoreIfField, reverseIfField, expressionLocal);
        }

        return (inDictionary, ignoreIfFields, reverseIfFields);
    }

    private void GenerateFieldInCode(InData<TFilter> inData, FieldInfo inDictionaryField, FieldInfo? ignoreIfField, FieldInfo? reverseIfField, LocalBuilder expressionLocal)
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
        _generator.Emit(OpCodes.Ldstr, inData.entityProperty.Name);
        _generator.Emit(OpCodes.Ldarg_0);
        _generator.Emit(OpCodes.Callvirt, inData.filterProperty.GetMethod);
        _generator.Emit(OpCodes.Ldsfld, inDictionaryField);
        _generator.Emit(OpCodes.Ldstr, inData.entityProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldOuterGetItem);
        _generator.Emit(OpCodes.Ldstr, inData.filterProperty.Name);
        _generator.Emit(OpCodes.Callvirt, CompareFieldInnerGetItem);
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
        if (inData.isCollection)
        {
            _generator.Emit(OpCodes.Call, BuildInCollectionExpressionMethod.MakeGenericMethod(inData.entityProperty.PropertyType, inData.filterPropertyItemType));
        }
        else
        {
            _generator.Emit(OpCodes.Call, BuildInArrayExpressionMethod.MakeGenericMethod(inData.entityProperty.PropertyType, inData.filterPropertyItemType));
        }

        if (ignoreIfField != null)
        {
            _generator.MarkLabel(endIfIgnore);
        }
    }
}
